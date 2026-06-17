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

/// <summary>1 file đầu vào để upload (Content phải seekable). Tách khỏi IFormFile để Application không phụ thuộc ASP.NET.</summary>
public sealed record UploadFile(string FileName, Stream Content, long Length);

/// <summary>Kết quả 1 file trong batch (SPEC §4.2 upload-batch).</summary>
public sealed record BatchUploadResultItem(int Index, string FileName, string Status, ImageDetailDto? Image, string? Error);

/// <summary>Tổng kết upload nhiều file → trả 207 Multi-Status.</summary>
public sealed record BatchUploadResponse(int Total, int Succeeded, int Failed, IReadOnlyList<BatchUploadResultItem> Results);
