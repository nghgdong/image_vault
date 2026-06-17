using ImageVault.Application.Abstractions;
using ImageVault.Domain.Entities;
using ImageVault.Infrastructure.Persistence;
using MongoDB.Driver;

namespace ImageVault.Infrastructure.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly IMongoCollection<User> _col;

    public UserRepository(MongoContext ctx) => _col = ctx.Users;

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default)
        => await _col.Find(u => u.Username == username).FirstOrDefaultAsync(ct);

    public async Task<bool> AnyAsync(CancellationToken ct = default)
        => await _col.Find(FilterDefinition<User>.Empty).AnyAsync(ct);

    public Task InsertAsync(User user, CancellationToken ct = default)
        => _col.InsertOneAsync(user, cancellationToken: ct);
}
