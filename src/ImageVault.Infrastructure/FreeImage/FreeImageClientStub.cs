using ImageVault.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace ImageVault.Infrastructure.FreeImage;

/// <summary>
/// STUB freeimage cho Phase 1 — trả dữ liệu giả, KHÔNG gọi mạng (SPEC §5, PLAN Phase 1).
/// Client thật (HttpClient + X-API-Key + Polly retry) sẽ thay ở Phase 2.
/// Interface KHÔNG có Delete vì Guest API không hỗ trợ xóa (SPEC §1.1).
/// </summary>
public sealed class FreeImageClientStub : IFreeImageClient
{
    private readonly ILogger<FreeImageClientStub> _logger;

    public FreeImageClientStub(ILogger<FreeImageClientStub> logger) => _logger = logger;

    public Task<FreeImageUploadResult> UploadAsync(Stream content, string fileName, CancellationToken ct = default)
    {
        _logger.LogWarning("FreeImageClientStub đang được dùng — KHÔNG upload thật. File: {FileName}", fileName);

        var fakeId = Guid.NewGuid().ToString("N")[..8];
        var ext = Path.GetExtension(fileName).TrimStart('.');
        if (string.IsNullOrEmpty(ext)) ext = "jpg";

        var result = new FreeImageUploadResult(
            Url: $"https://iili.io/{fakeId}.{ext}",
            ThumbUrl: $"https://iili.io/{fakeId}.th.{ext}",
            MediumUrl: $"https://iili.io/{fakeId}.md.{ext}",
            Width: 1920,
            Height: 1080,
            SizeBytes: content.CanSeek ? content.Length : null,
            MimeType: $"image/{(ext == "jpg" ? "jpeg" : ext)}",
            FreeImageId: fakeId);

        return Task.FromResult(result);
    }
}
