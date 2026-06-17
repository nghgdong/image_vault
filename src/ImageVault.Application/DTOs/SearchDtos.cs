namespace ImageVault.Application.DTOs;

public sealed record FolderHit(string Id, string Name, string? ParentId);

public sealed record ImageHit(
    string Id,
    string Name,
    string FolderId,
    string Url,
    string ThumbUrl,
    int? Width,
    int? Height);

/// <summary>Kết quả tìm kiếm toàn kho theo tên (folder + ảnh).</summary>
public sealed record SearchResults(
    string Query,
    IReadOnlyList<FolderHit> Folders,
    IReadOnlyList<ImageHit> Images);
