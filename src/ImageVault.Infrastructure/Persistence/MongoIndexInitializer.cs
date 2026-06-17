using ImageVault.Domain.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace ImageVault.Infrastructure.Persistence;

/// <summary>Đăng ký index lúc khởi động (SPEC §3). Idempotent — chạy lại không lỗi.</summary>
public sealed class MongoIndexInitializer : IHostedService
{
    private readonly MongoContext _ctx;
    private readonly ILogger<MongoIndexInitializer> _logger;

    public MongoIndexInitializer(MongoContext ctx, ILogger<MongoIndexInitializer> logger)
    {
        _ctx = ctx;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // folders: { parentId, isDeleted }, { path }, text name
        var fk = Builders<Folder>.IndexKeys;
        await _ctx.Folders.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<Folder>(fk.Ascending(f => f.ParentId).Ascending(f => f.IsDeleted)),
            new CreateIndexModel<Folder>(fk.Ascending(f => f.Path)),
            new CreateIndexModel<Folder>(fk.Text(f => f.Name)),
        }, cancellationToken);

        // images: { folderId, isDeleted }, text name
        var ik = Builders<ImageItem>.IndexKeys;
        await _ctx.Images.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<ImageItem>(ik.Ascending(i => i.FolderId).Ascending(i => i.IsDeleted)),
            new CreateIndexModel<ImageItem>(ik.Text(i => i.Name)),
        }, cancellationToken);

        // users: unique username
        await _ctx.Users.Indexes.CreateOneAsync(
            new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Ascending(u => u.Username),
                new CreateIndexOptions { Unique = true }),
            cancellationToken: cancellationToken);

        _logger.LogInformation("Đã đăng ký Mongo indexes.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
