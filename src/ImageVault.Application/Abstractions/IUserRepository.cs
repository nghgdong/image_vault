using ImageVault.Domain.Entities;

namespace ImageVault.Application.Abstractions;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default);
    Task<bool> AnyAsync(CancellationToken ct = default);
    Task InsertAsync(User user, CancellationToken ct = default);
}
