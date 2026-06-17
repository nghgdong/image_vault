using ImageVault.Application.Abstractions;
using ImageVault.Application.Common;
using ImageVault.Application.DTOs;
using ImageVault.Domain.Entities;

namespace ImageVault.Application.Services;

/// <summary>
/// Logic cây thư mục materialized path — SPEC §3.1 (phần dễ sai nhất).
/// Public: GetRoot/GetChildren/GetBreadcrumb. Mutations (create/rename/move/delete)
/// được implement ở đây để unit test ngay Phase 1; controller admin nối ở Phase 2.
/// </summary>
public sealed class FolderService
{
    private const string RootPath = "/"; // path "cha" của các folder top-level (root ảo)

    private readonly IFolderRepository _folders;
    private readonly IImageRepository _images;
    private readonly IClock _clock;
    private readonly IIdGenerator _ids;

    public FolderService(IFolderRepository folders, IImageRepository images, IClock clock, IIdGenerator ids)
    {
        _folders = folders;
        _images = images;
        _clock = clock;
        _ids = ids;
    }

    // ---------- Mutations (logic path) ----------

    public async Task<Folder> CreateFolderAsync(string name, string? parentId, CancellationToken ct = default)
    {
        name = NormalizeName(name);

        Folder? parent = null;
        if (parentId is not null)
        {
            parent = await _folders.GetByIdAsync(parentId, ct: ct)
                     ?? throw new NotFoundException($"Không tìm thấy thư mục cha '{parentId}'.");
        }

        if (await _folders.ExistsByNameInParentAsync(parentId, name, excludeId: null, ct))
            throw new ConflictException($"Đã tồn tại thư mục tên '{name}' trong thư mục này.");

        var id = _ids.NewId();
        var parentPath = parent?.Path ?? RootPath;
        var now = _clock.UtcNow;

        var folder = new Folder
        {
            Id = id,
            Name = name,
            ParentId = parentId,
            Path = parentPath + id + "/",
            Depth = parent is not null ? parent.Depth + 1 : 0,
            IsDeleted = false,
            CreatedAt = now,
            UpdatedAt = now,
        };

        await _folders.InsertAsync(folder, ct);
        return folder;
    }

    public async Task<Folder> RenameFolderAsync(string id, string newName, CancellationToken ct = default)
    {
        newName = NormalizeName(newName);
        var folder = await _folders.GetByIdAsync(id, ct: ct)
                     ?? throw new NotFoundException($"Không tìm thấy thư mục '{id}'.");

        if (folder.Name == newName) return folder;

        if (await _folders.ExistsByNameInParentAsync(folder.ParentId, newName, excludeId: id, ct))
            throw new ConflictException($"Đã tồn tại thư mục tên '{newName}' trong thư mục này.");

        folder.Name = newName;
        folder.UpdatedAt = _clock.UtcNow;
        await _folders.ReplaceAsync(folder, ct);
        return folder;
    }

    /// <summary>
    /// Move folder: cập nhật path+depth của CHÍNH NÓ và TOÀN BỘ con cháu (SPEC §3.1).
    /// Chặn move vào chính nó hoặc con cháu của nó (vòng lặp) → 400.
    /// </summary>
    public async Task<Folder> MoveFolderAsync(string id, string? newParentId, CancellationToken ct = default)
    {
        var folder = await _folders.GetByIdAsync(id, ct: ct)
                     ?? throw new NotFoundException($"Không tìm thấy thư mục '{id}'.");

        if (newParentId == folder.ParentId) return folder; // không đổi chỗ

        Folder? newParent = null;
        if (newParentId is not null)
        {
            if (newParentId == id)
                throw new ValidationAppException("Không thể di chuyển thư mục vào chính nó.");

            newParent = await _folders.GetByIdAsync(newParentId, ct: ct)
                        ?? throw new NotFoundException($"Không tìm thấy thư mục đích '{newParentId}'.");

            // newParent là con cháu của folder ⇒ path của nó bắt đầu bằng path của folder ⇒ vòng lặp.
            if (newParent.Path.StartsWith(folder.Path, StringComparison.Ordinal))
                throw new ValidationAppException("Không thể di chuyển thư mục vào con cháu của chính nó.");
        }

        if (await _folders.ExistsByNameInParentAsync(newParentId, folder.Name, excludeId: id, ct))
            throw new ConflictException($"Thư mục đích đã có mục tên '{folder.Name}'.");

        var oldPath = folder.Path;
        var newParentPath = newParent?.Path ?? RootPath;
        var newDepth = newParent is not null ? newParent.Depth + 1 : 0;
        var newPath = newParentPath + id + "/";
        var depthDelta = newDepth - folder.Depth;
        var now = _clock.UtcNow;

        // Rebase path+depth cho chính nó + con cháu (bulk theo prefix).
        await _folders.RebasePathAsync(oldPath, newPath, depthDelta, now, ct);

        // Persist parentId mới cho chính nó (rebase không đổi parentId).
        folder.ParentId = newParentId;
        folder.Path = newPath;
        folder.Depth = newDepth;
        folder.UpdatedAt = now;
        await _folders.ReplaceAsync(folder, ct);
        return folder;
    }

    /// <summary>Soft-delete đệ quy: folder + toàn bộ con cháu (folders & images). KHÔNG xóa binary freeimage (SPEC §1.1).</summary>
    public async Task<DeleteFolderResult> SoftDeleteFolderAsync(string id, bool cascade = true, CancellationToken ct = default)
    {
        var folder = await _folders.GetByIdAsync(id, ct: ct)
                     ?? throw new NotFoundException($"Không tìm thấy thư mục '{id}'.");

        if (!cascade)
        {
            var hasSub = (await _folders.GetChildrenAsync(id, ct)).Count > 0;
            var imgCount = (await _images.CountByFolderIdsAsync(new[] { id }, ct)).GetValueOrDefault(id);
            if (hasSub || imgCount > 0)
                throw new ConflictException("Thư mục còn nội dung. Dùng cascade=true để xóa đệ quy.");
        }

        var now = _clock.UtcNow;
        var deletedFolderIds = await _folders.SoftDeleteByPathPrefixAsync(folder.Path, now, ct);
        var deletedImages = await _images.SoftDeleteByFolderIdsAsync(deletedFolderIds, now, ct);
        return new DeleteFolderResult(deletedFolderIds.Count, deletedImages);
    }

    /// <summary>Lấy 1 folder (chưa xóa) hoặc 404.</summary>
    public async Task<Folder> GetFolderAsync(string id, CancellationToken ct = default)
        => await _folders.GetByIdAsync(id, ct: ct)
           ?? throw new NotFoundException($"Không tìm thấy thư mục '{id}'.");

    // ---------- Public queries (SPEC §4.1) ----------

    /// <summary>Root ảo: trả các folder top-level làm subFolders (ảnh luôn thuộc folder thật → images rỗng).</summary>
    public async Task<FolderChildrenDto> GetRootAsync(CancellationToken ct = default)
    {
        var subs = await BuildSubFolderDtosAsync(parentId: null, ct);
        return new FolderChildrenDto(
            Folder: new FolderSummaryDto(null, "Root", null),
            SubFolders: subs,
            Images: Array.Empty<ImageDto>(),
            Page: 1, PageSize: 0, TotalImages: 0);
    }

    public async Task<FolderChildrenDto> GetChildrenAsync(string folderId, ContentQuery query, CancellationToken ct = default)
    {
        var folder = await _folders.GetByIdAsync(folderId, ct: ct)
                     ?? throw new NotFoundException($"Không tìm thấy thư mục '{folderId}'.");

        var subs = await BuildSubFolderDtosAsync(folderId, ct);

        var q = query.Normalized();
        var paged = await _images.GetByFolderAsync(folderId, q, ct);
        var imageDtos = paged.Items
            .Select(i => new ImageDto(i.Id, i.Name, i.Url, i.ThumbUrl, i.Width, i.Height))
            .ToList();

        return new FolderChildrenDto(
            Folder: new FolderSummaryDto(folder.Id, folder.Name, folder.ParentId),
            SubFolders: subs,
            Images: imageDtos,
            Page: q.Page, PageSize: q.PageSize, TotalImages: paged.Total);
    }

    public async Task<IReadOnlyList<BreadcrumbItemDto>> GetBreadcrumbAsync(string folderId, CancellationToken ct = default)
    {
        var folder = await _folders.GetByIdAsync(folderId, ct: ct)
                     ?? throw new NotFoundException($"Không tìm thấy thư mục '{folderId}'.");

        // Tách ObjectId từ path "/a/b/c/" → [a,b,c] (theo thứ tự từ root tới hiện tại).
        var ids = folder.Path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var map = (await _folders.GetManyByIdsAsync(ids, ct)).ToDictionary(f => f.Id, f => f.Name);

        var crumbs = new List<BreadcrumbItemDto>(ids.Length);
        foreach (var fid in ids)
            if (map.TryGetValue(fid, out var name))
                crumbs.Add(new BreadcrumbItemDto(fid, name));
        return crumbs;
    }

    private async Task<IReadOnlyList<SubFolderDto>> BuildSubFolderDtosAsync(string? parentId, CancellationToken ct)
    {
        var subs = await _folders.GetChildrenAsync(parentId, ct);
        if (subs.Count == 0) return Array.Empty<SubFolderDto>();

        var subIds = subs.Select(s => s.Id).ToArray();
        var imageCounts = await _images.CountByFolderIdsAsync(subIds, ct);
        var subFolderCounts = await _folders.CountChildrenByParentIdsAsync(subIds, ct);

        return subs
            .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .Select(s => new SubFolderDto(
                s.Id, s.Name,
                imageCounts.GetValueOrDefault(s.Id),
                subFolderCounts.GetValueOrDefault(s.Id)))
            .ToList();
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ValidationAppException("Tên thư mục không được để trống.");
        name = name.Trim();
        if (name.Contains('/'))
            throw new ValidationAppException("Tên thư mục không được chứa ký tự '/'.");
        return name;
    }
}
