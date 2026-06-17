namespace ImageVault.Infrastructure.Seed;

public sealed class AdminOptions
{
    public const string SectionName = "Admin";

    public string Username { get; set; } = "admin";

    /// <summary>Đọc từ env ADMIN__PASSWORD. KHÔNG hardcode (SPEC §8). Phải đổi ngay.</summary>
    public string Password { get; set; } = string.Empty;
}
