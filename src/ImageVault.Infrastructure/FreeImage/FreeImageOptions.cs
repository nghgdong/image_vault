namespace ImageVault.Infrastructure.FreeImage;

public sealed class FreeImageOptions
{
    public const string SectionName = "FreeImage";

    /// <summary>API key freeimage (dạng chv_...). CHỈ ở backend, đọc từ env (SPEC §1.2, §7, §8).</summary>
    public string ApiKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = "https://freeimage.host/api/1/upload";
}
