using ImageVault.Application.DTOs;

namespace ImageVault.Application.Common;

public sealed record ImageValidationResult(bool Ok, string? Error, string? Mime)
{
    public static ImageValidationResult Fail(string error) => new(false, error, null);
    public static ImageValidationResult Success(string mime) => new(true, null, mime);
}

/// <summary>
/// Validate file upload bằng MAGIC BYTES (không chỉ tin extension) + size — SPEC §5.4.3, §7.
/// </summary>
public static class ImageFileValidator
{
    public const long MaxBytes = 64L * 1024 * 1024; // 64MB
    public const int MaxBatchFiles = 20;

    public static ImageValidationResult Validate(UploadFile file)
    {
        if (file.Length <= 0)
            return ImageValidationResult.Fail($"File '{file.FileName}' rỗng.");
        if (file.Length > MaxBytes)
            return ImageValidationResult.Fail($"File '{file.FileName}' vượt quá 64MB.");

        var sig = ReadSignature(file.Content, 16);
        var mime = DetectImageMime(sig);
        if (mime is null)
            return ImageValidationResult.Fail($"File '{file.FileName}' không phải định dạng ảnh hợp lệ.");

        return ImageValidationResult.Success(mime);
    }

    private static byte[] ReadSignature(Stream stream, int count)
    {
        var pos = stream.CanSeek ? stream.Position : 0;
        if (stream.CanSeek) stream.Position = 0;

        var buffer = new byte[count];
        var read = 0;
        while (read < count)
        {
            var n = stream.Read(buffer, read, count - read);
            if (n == 0) break;
            read += n;
        }

        if (stream.CanSeek) stream.Position = pos;
        return read == count ? buffer : buffer[..read];
    }

    private static string? DetectImageMime(byte[] b)
    {
        // JPEG: FF D8 FF
        if (b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF)
            return "image/jpeg";
        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (b.Length >= 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47
            && b[4] == 0x0D && b[5] == 0x0A && b[6] == 0x1A && b[7] == 0x0A)
            return "image/png";
        // GIF: "GIF8"
        if (b.Length >= 4 && b[0] == 0x47 && b[1] == 0x49 && b[2] == 0x46 && b[3] == 0x38)
            return "image/gif";
        // BMP: "BM"
        if (b.Length >= 2 && b[0] == 0x42 && b[1] == 0x4D)
            return "image/bmp";
        // WEBP: "RIFF"...."WEBP"
        if (b.Length >= 12 && b[0] == 0x52 && b[1] == 0x49 && b[2] == 0x46 && b[3] == 0x46
            && b[8] == 0x57 && b[9] == 0x45 && b[10] == 0x42 && b[11] == 0x50)
            return "image/webp";
        return null;
    }
}
