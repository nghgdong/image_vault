namespace ImageVault.Application.Abstractions;

/// <summary>
/// Kết quả upload đã được map từ response freeimage.host (SPEC §5.2).
/// </summary>
public sealed record FreeImageUploadResult(
    string Url,
    string ThumbUrl,
    string? MediumUrl,
    int? Width,
    int? Height,
    long? SizeBytes,
    string? MimeType,
    string? FreeImageId);

/// <summary>
/// Cổng tích hợp freeimage.host (SPEC §5).
/// CHÚ Ý (SPEC §1.1): Guest API KHÔNG có endpoint xóa ảnh → interface này
/// CỐ TÌNH không có phương thức Delete. Mọi "xóa" là soft delete metadata ở Mongo.
/// Upload LUÔN đi qua backend, API key chỉ ở server (SPEC §1.2, §7).
/// </summary>
public interface IFreeImageClient
{
    Task<FreeImageUploadResult> UploadAsync(Stream content, string fileName, CancellationToken ct = default);
}
