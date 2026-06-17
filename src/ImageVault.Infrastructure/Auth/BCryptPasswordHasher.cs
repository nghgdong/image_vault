using ImageVault.Application.Abstractions;

namespace ImageVault.Infrastructure.Auth;

public sealed class BCryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);

    public bool Verify(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch
        {
            // Hash không hợp lệ → coi như sai mật khẩu (không ném ra ngoài).
            return false;
        }
    }
}
