using ImageVault.Application.Abstractions;
using ImageVault.Domain.Common;
using ImageVault.Domain.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ImageVault.Infrastructure.Seed;

/// <summary>Seed 1 admin lúc khởi động nếu collection users rỗng (SPEC §3.3). BCrypt hash.</summary>
public sealed class AdminSeeder : IHostedService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly IClock _clock;
    private readonly IIdGenerator _ids;
    private readonly AdminOptions _options;
    private readonly ILogger<AdminSeeder> _logger;

    public AdminSeeder(
        IUserRepository users,
        IPasswordHasher hasher,
        IClock clock,
        IIdGenerator ids,
        IOptions<AdminOptions> options,
        ILogger<AdminSeeder> logger)
    {
        _users = users;
        _hasher = hasher;
        _clock = clock;
        _ids = ids;
        _options = options.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (await _users.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Đã có user — bỏ qua seed admin.");
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.Password))
        {
            _logger.LogWarning(
                "ADMIN__PASSWORD trống — KHÔNG seed admin. Hãy đặt biến môi trường ADMIN__USERNAME/ADMIN__PASSWORD.");
            return;
        }

        var now = _clock.UtcNow;
        var admin = new User
        {
            Id = _ids.NewId(),
            Username = _options.Username,
            PasswordHash = _hasher.Hash(_options.Password),
            Role = Roles.Admin,
            CreatedAt = now,
        };

        await _users.InsertAsync(admin, cancellationToken);
        _logger.LogInformation("Đã seed admin '{Username}'.", _options.Username);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
