namespace ImageVault.Domain.Entities;

/// <summary>Tài khoản admin — SPEC §3.3. v1 chỉ 1 admin, seed từ config.</summary>
public class User
{
    public string Id { get; set; } = default!;

    /// <summary>Unique.</summary>
    public string Username { get; set; } = default!;

    /// <summary>BCrypt hash. KHÔNG bao giờ lưu plaintext.</summary>
    public string PasswordHash { get; set; } = default!;

    /// <summary>"Admin".</summary>
    public string Role { get; set; } = default!;

    public DateTime CreatedAt { get; set; }
}
