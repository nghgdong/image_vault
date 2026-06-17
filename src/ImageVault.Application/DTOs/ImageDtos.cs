namespace ImageVault.Application.DTOs;

/// <summary>Ảnh hiển thị trong lưới (SPEC §4.1).</summary>
public sealed record ImageDto(
    string Id,
    string Name,
    string Url,
    string ThumbUrl,
    int? Width,
    int? Height);

/// <summary>Chi tiết 1 ảnh cho lightbox (SPEC §4.1 GET /images/{id}).</summary>
public sealed record ImageDetailDto(
    string Id,
    string FolderId,
    string Name,
    string Url,
    string ThumbUrl,
    string? MediumUrl,
    int? Width,
    int? Height,
    long? SizeBytes,
    string? MimeType,
    DateTime UploadedAt);

// --- Request bodies (Phase 2) ---
public sealed record UpdateImageRequest(string? Name, string? FolderId);
