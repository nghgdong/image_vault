using ImageVault.Application.Common;
using ImageVault.Domain.Entities;

namespace ImageVault.Application.Abstractions;

public interface IImageRepository
{
    Task<ImageItem?> GetByIdAsync(string id, bool includeDeleted = false, CancellationToken ct = default);

    /// <summary>Ảnh (chưa xóa) trong 1 folder, đã phân trang + sort.</summary>
    Task<PagedResult<ImageItem>> GetByFolderAsync(string folderId, ContentQuery query, CancellationToken ct = default);

    /// <summary>Đếm số ảnh (chưa xóa) cho từng folderId.</summary>
    Task<IReadOnlyDictionary<string, long>> CountByFolderIdsAsync(IReadOnlyCollection<string> folderIds, CancellationToken ct = default);

    Task InsertAsync(ImageItem image, CancellationToken ct = default);

    Task ReplaceAsync(ImageItem image, CancellationToken ct = default);

    /// <summary>Soft-delete 1 ảnh. KHÔNG gọi freeimage xóa binary (SPEC §1.1).</summary>
    Task<bool> SoftDeleteAsync(string id, DateTime now, CancellationToken ct = default);

    /// <summary>Soft-delete tất cả ảnh thuộc các folder bị xóa (dùng cho xóa folder đệ quy).</summary>
    Task<long> SoftDeleteByFolderIdsAsync(IReadOnlyCollection<string> folderIds, DateTime now, CancellationToken ct = default);
}
