using ImageVault.Domain.Entities;

namespace ImageVault.Application.Abstractions;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public sealed record IssuedToken(string Token, DateTime ExpiresAt);

public interface IJwtTokenService
{
    IssuedToken Generate(User user);
}
