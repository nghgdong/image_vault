using ImageVault.Domain.Entities;

namespace ImageVault.Application.Abstractions;

public interface IFolderRepository
{
    Task<Folder?> GetByIdAsync(string id, bool includeDeleted = false, CancellationToken ct = default);

    /// <summary>Folder con trực tiếp (chưa xóa). parentId null = top-level.</summary>
    Task<IReadOnlyList<Folder>> GetChildrenAsync(string? parentId, CancellationToken ct = default);

    /// <summary>Lấy nhiều folder theo danh sách id (cho breadcrumb), bỏ qua đã xóa.</summary>
    Task<IReadOnlyList<Folder>> GetManyByIdsAsync(IEnumerable<string> ids, CancellationToken ct = default);

    /// <summary>Tìm folder (chưa xóa) có tên chứa <paramref name="query"/> (không phân biệt hoa/thường).</summary>
    Task<IReadOnlyList<Folder>> SearchByNameAsync(string query, int limit, CancellationToken ct = default);

    /// <summary>Có folder (chưa xóa) trùng tên trong cùng parent không (loại trừ excludeId).</summary>
    Task<bool> ExistsByNameInParentAsync(string? parentId, string name, string? excludeId, CancellationToken ct = default);

    /// <summary>Đếm số folder con trực tiếp (chưa xóa) cho từng parentId.</summary>
    Task<IReadOnlyDictionary<string, long>> CountChildrenByParentIdsAsync(IReadOnlyCollection<string> parentIds, CancellationToken ct = default);

    Task InsertAsync(Folder folder, CancellationToken ct = default);

    /// <summary>Cập nhật trực tiếp 1 document (rename / đổi parentId của chính nó).</summary>
    Task ReplaceAsync(Folder folder, CancellationToken ct = default);

    /// <summary>
    /// Move: đổi path-prefix cho TẤT CẢ folder có path bắt đầu bằng <paramref name="oldPathPrefix"/>
    /// (gồm CHÍNH NÓ + toàn bộ con cháu), cộng <paramref name="depthDelta"/> vào depth. SPEC §3.1.
    /// Trả số document bị ảnh hưởng.
    /// </summary>
    Task<long> RebasePathAsync(string oldPathPrefix, string newPathPrefix, int depthDelta, DateTime now, CancellationToken ct = default);

    /// <summary>Soft-delete đệ quy: mọi folder có path bắt đầu bằng prefix (gồm chính nó). Trả id các folder bị xóa.</summary>
    Task<IReadOnlyList<string>> SoftDeleteByPathPrefixAsync(string pathPrefix, DateTime now, CancellationToken ct = default);
}
