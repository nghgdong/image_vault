using ImageVault.Application.Abstractions;
using ImageVault.Application.Common;
using ImageVault.Domain.Entities;

namespace ImageVault.UnitTests;

internal sealed class FakeClock : IClock
{
    public DateTime UtcNow { get; set; } = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
}

internal sealed class SequentialIdGenerator : IIdGenerator
{
    private int _n;
    public string NewId() => $"id{++_n:D2}";
}

/// <summary>
/// In-memory folder repo mô phỏng đúng ngữ nghĩa materialized path (prefix replace),
/// để test logic move/delete đệ quy của FolderService một cách thực chất.
/// </summary>
internal sealed class InMemoryFolderRepository : IFolderRepository
{
    public readonly Dictionary<string, Folder> Store = new();

    public Task<Folder?> GetByIdAsync(string id, bool includeDeleted = false, CancellationToken ct = default)
    {
        Store.TryGetValue(id, out var f);
        if (f is null || (!includeDeleted && f.IsDeleted)) return Task.FromResult<Folder?>(null);
        return Task.FromResult<Folder?>(f);
    }

    public Task<IReadOnlyList<Folder>> GetChildrenAsync(string? parentId, CancellationToken ct = default)
    {
        var list = Store.Values
            .Where(f => !f.IsDeleted && f.ParentId == parentId)
            .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return Task.FromResult<IReadOnlyList<Folder>>(list);
    }

    public Task<IReadOnlyList<Folder>> GetManyByIdsAsync(IEnumerable<string> ids, CancellationToken ct = default)
    {
        var set = ids.ToHashSet();
        var list = Store.Values.Where(f => !f.IsDeleted && set.Contains(f.Id)).ToList();
        return Task.FromResult<IReadOnlyList<Folder>>(list);
    }

    public Task<bool> ExistsByNameInParentAsync(string? parentId, string name, string? excludeId, CancellationToken ct = default)
        => Task.FromResult(Store.Values.Any(f =>
            !f.IsDeleted && f.ParentId == parentId && f.Name == name && f.Id != excludeId));

    public Task<IReadOnlyDictionary<string, long>> CountChildrenByParentIdsAsync(
        IReadOnlyCollection<string> parentIds, CancellationToken ct = default)
    {
        var dict = Store.Values
            .Where(f => !f.IsDeleted && f.ParentId is not null && parentIds.Contains(f.ParentId))
            .GroupBy(f => f.ParentId!)
            .ToDictionary(g => g.Key, g => (long)g.Count());
        return Task.FromResult<IReadOnlyDictionary<string, long>>(dict);
    }

    public Task InsertAsync(Folder folder, CancellationToken ct = default)
    {
        Store[folder.Id] = folder;
        return Task.CompletedTask;
    }

    public Task ReplaceAsync(Folder folder, CancellationToken ct = default)
    {
        Store[folder.Id] = folder;
        return Task.CompletedTask;
    }

    public Task<long> RebasePathAsync(string oldPathPrefix, string newPathPrefix, int depthDelta, DateTime now, CancellationToken ct = default)
    {
        long n = 0;
        foreach (var f in Store.Values.Where(f => f.Path.StartsWith(oldPathPrefix, StringComparison.Ordinal)))
        {
            f.Path = newPathPrefix + f.Path[oldPathPrefix.Length..];
            f.Depth += depthDelta;
            f.UpdatedAt = now;
            n++;
        }
        return Task.FromResult(n);
    }

    public Task<IReadOnlyList<string>> SoftDeleteByPathPrefixAsync(string pathPrefix, DateTime now, CancellationToken ct = default)
    {
        var ids = new List<string>();
        foreach (var f in Store.Values.Where(f => !f.IsDeleted && f.Path.StartsWith(pathPrefix, StringComparison.Ordinal)))
        {
            f.IsDeleted = true;
            f.UpdatedAt = now;
            ids.Add(f.Id);
        }
        return Task.FromResult<IReadOnlyList<string>>(ids);
    }
}

/// <summary>Fake freeimage: đếm số lần upload, trả dữ liệu giả; ném lỗi nếu tên chứa "boom".</summary>
internal sealed class FakeFreeImageClient : IFreeImageClient
{
    public int UploadCount { get; private set; }
    public readonly List<string> UploadedFiles = new();

    public Task<FreeImageUploadResult> UploadAsync(Stream content, string fileName, CancellationToken ct = default)
    {
        UploadCount++;
        UploadedFiles.Add(fileName);
        if (fileName.Contains("boom", StringComparison.OrdinalIgnoreCase))
            throw new UpstreamException($"freeimage giả lập lỗi cho '{fileName}'.");

        var id = $"fake{UploadCount:D3}";
        return Task.FromResult(new FreeImageUploadResult(
            Url: $"https://iili.io/{id}.png",
            ThumbUrl: $"https://iili.io/{id}.th.png",
            MediumUrl: $"https://iili.io/{id}.md.png",
            Width: 800, Height: 600,
            SizeBytes: content.CanSeek ? content.Length : null,
            MimeType: "image/png",
            FreeImageId: id));
    }
}

internal sealed class InMemoryImageRepository : IImageRepository
{
    public readonly List<ImageItem> Store = new();

    public Task<ImageItem?> GetByIdAsync(string id, bool includeDeleted = false, CancellationToken ct = default)
        => Task.FromResult(Store.FirstOrDefault(i => i.Id == id && (includeDeleted || !i.IsDeleted)));

    public Task<PagedResult<ImageItem>> GetByFolderAsync(string folderId, ContentQuery query, CancellationToken ct = default)
    {
        var q = query.Normalized();
        var all = Store.Where(i => !i.IsDeleted && i.FolderId == folderId).ToList();
        var items = all.Skip((q.Page - 1) * q.PageSize).Take(q.PageSize).ToList();
        return Task.FromResult(new PagedResult<ImageItem> { Items = items, Total = all.Count, Page = q.Page, PageSize = q.PageSize });
    }

    public Task<IReadOnlyDictionary<string, long>> CountByFolderIdsAsync(
        IReadOnlyCollection<string> folderIds, CancellationToken ct = default)
    {
        var dict = Store
            .Where(i => !i.IsDeleted && folderIds.Contains(i.FolderId))
            .GroupBy(i => i.FolderId)
            .ToDictionary(g => g.Key, g => (long)g.Count());
        return Task.FromResult<IReadOnlyDictionary<string, long>>(dict);
    }

    public Task InsertAsync(ImageItem image, CancellationToken ct = default)
    {
        Store.Add(image);
        return Task.CompletedTask;
    }

    public Task ReplaceAsync(ImageItem image, CancellationToken ct = default)
    {
        Store.RemoveAll(i => i.Id == image.Id);
        Store.Add(image);
        return Task.CompletedTask;
    }

    public Task<bool> SoftDeleteAsync(string id, DateTime now, CancellationToken ct = default)
    {
        var img = Store.FirstOrDefault(i => i.Id == id && !i.IsDeleted);
        if (img is null) return Task.FromResult(false);
        img.IsDeleted = true;
        img.UpdatedAt = now;
        return Task.FromResult(true);
    }

    public Task<long> SoftDeleteByFolderIdsAsync(IReadOnlyCollection<string> folderIds, DateTime now, CancellationToken ct = default)
    {
        long n = 0;
        foreach (var img in Store.Where(i => !i.IsDeleted && folderIds.Contains(i.FolderId)))
        {
            img.IsDeleted = true;
            img.UpdatedAt = now;
            n++;
        }
        return Task.FromResult(n);
    }
}
