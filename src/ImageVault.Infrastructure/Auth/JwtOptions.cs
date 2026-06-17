namespace ImageVault.Infrastructure.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Secret { get; set; } = default!;
    public string Issuer { get; set; } = "image-vault";
    public string? Audience { get; set; }
    public int ExpiryHours { get; set; } = 8;
}
