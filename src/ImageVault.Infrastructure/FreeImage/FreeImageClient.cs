using System.Net.Http.Headers;
using System.Text.Json;
using ImageVault.Application.Abstractions;
using ImageVault.Application.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ImageVault.Infrastructure.FreeImage;

/// <summary>
/// Client thật gọi freeimage.host (Chevereto guest API) — SPEC §5.
/// Upload qua multipart, API key chỉ ở server (header X-API-Key + field key).
/// CHÚ Ý §1.1: KHÔNG có Delete — guest API không hỗ trợ xóa binary.
/// Retry tạm thời do Polly cấu hình ở DI (3 lần, exponential backoff).
/// </summary>
public sealed class FreeImageClient : IFreeImageClient
{
    private readonly HttpClient _http;
    private readonly FreeImageOptions _options;
    private readonly ILogger<FreeImageClient> _logger;

    public FreeImageClient(HttpClient http, IOptions<FreeImageOptions> options, ILogger<FreeImageClient> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<FreeImageUploadResult> UploadAsync(Stream content, string fileName, CancellationToken ct = default)
    {
        using var form = new MultipartFormDataContent();
        var fileContent = new StreamContent(content);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        form.Add(fileContent, "source", fileName);
        form.Add(new StringContent("json"), "format");
        form.Add(new StringContent(_options.ApiKey), "key"); // guest API nhận key qua field

        using var req = new HttpRequestMessage(HttpMethod.Post, _options.BaseUrl) { Content = form };
        req.Headers.Add("X-API-Key", _options.ApiKey);

        HttpResponseMessage resp;
        try
        {
            resp = await _http.SendAsync(req, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            throw new UpstreamException($"Không gọi được freeimage.host: {ex.Message}");
        }

        var json = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
            throw new UpstreamException($"freeimage.host trả HTTP {(int)resp.StatusCode}: {Truncate(json)}");

        try
        {
            return Parse(json);
        }
        catch (UpstreamException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Không parse được response freeimage: {Json}", Truncate(json));
            throw new UpstreamException("Không đọc được phản hồi từ freeimage.host.");
        }
    }

    private static FreeImageUploadResult Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (TryGetInt(root, "status_code") is int code && code != 200)
        {
            var txt = root.TryGetProperty("status_txt", out var st) ? st.GetString() : null;
            throw new UpstreamException($"freeimage.host lỗi (status_code={code}): {txt}");
        }

        if (!root.TryGetProperty("image", out var image))
            throw new UpstreamException("Response freeimage thiếu trường 'image'.");

        var url = image.TryGetProperty("url", out var u) ? u.GetString() : null;
        if (string.IsNullOrEmpty(url))
            throw new UpstreamException("Response freeimage thiếu 'image.url'.");

        var thumb = GetNestedUrl(image, "thumb") ?? url;       // fallback = url (SPEC §5.2)
        var medium = GetNestedUrl(image, "medium");

        return new FreeImageUploadResult(
            Url: url,
            ThumbUrl: thumb,
            MediumUrl: medium,
            Width: TryGetInt(image, "width"),
            Height: TryGetInt(image, "height"),
            SizeBytes: TryGetLong(image, "size"),
            MimeType: image.TryGetProperty("mime", out var m) ? m.GetString() : null,
            FreeImageId: image.TryGetProperty("id_encoded", out var idc) ? idc.GetString() : null);
    }

    private static string? GetNestedUrl(JsonElement parent, string name)
        => parent.TryGetProperty(name, out var child) && child.ValueKind == JsonValueKind.Object
           && child.TryGetProperty("url", out var u) ? u.GetString() : null;

    // Chevereto đôi khi trả số dưới dạng string → chấp nhận cả Number lẫn String.
    private static int? TryGetInt(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var p)) return null;
        if (p.ValueKind == JsonValueKind.Number && p.TryGetInt32(out var n)) return n;
        if (p.ValueKind == JsonValueKind.String && int.TryParse(p.GetString(), out var s)) return s;
        return null;
    }

    private static long? TryGetLong(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var p)) return null;
        if (p.ValueKind == JsonValueKind.Number && p.TryGetInt64(out var n)) return n;
        if (p.ValueKind == JsonValueKind.String && long.TryParse(p.GetString(), out var s)) return s;
        return null;
    }

    private static string Truncate(string s, int max = 300)
        => s.Length <= max ? s : s[..max] + "…";
}
