namespace ImageVault.Application.DTOs;

/// <summary>Mô tả ngắn 1 folder (dùng trong response children & breadcrumb).</summary>
public sealed record FolderSummaryDto(string? Id, string Name, string? ParentId);

/// <summary>Folder con kèm số lượng (SPEC §4.1).</summary>
public sealed record SubFolderDto(string Id, string Name, long ImageCount, long SubFolderCount);

/// <summary>1 mục trong breadcrumb.</summary>
public sealed record BreadcrumbItemDto(string Id, string Name);

/// <summary>Nội dung 1 thư mục: folder hiện tại + folder con + ảnh (đã phân trang).</summary>
public sealed record FolderChildrenDto(
    FolderSummaryDto Folder,
    IReadOnlyList<SubFolderDto> SubFolders,
    IReadOnlyList<ImageDto> Images,
    int Page,
    int PageSize,
    long TotalImages);

// --- Request bodies (dùng ở Phase 2 cho endpoint admin, định nghĩa sẵn) ---

public sealed record CreateFolderRequest(string Name, string? ParentId);

public sealed record UpdateFolderRequest(string? Name, string? ParentId);

/// <summary>Kết quả tạo folder.</summary>
public sealed record FolderCreatedDto(string Id, string Name, string? ParentId, string Path);

/// <summary>Kết quả soft-delete đệ quy.</summary>
public sealed record DeleteFolderResult(long DeletedFolders, long DeletedImages);
