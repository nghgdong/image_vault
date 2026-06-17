using System.Text.RegularExpressions;
using ImageVault.Application.Abstractions;
using ImageVault.Application.Common;
using ImageVault.Domain.Entities;
using ImageVault.Infrastructure.Persistence;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ImageVault.Infrastructure.Repositories;

public sealed class ImageRepository : IImageRepository
{
    private readonly IMongoCollection<ImageItem> _col;
    private static readonly FilterDefinitionBuilder<ImageItem> F = Builders<ImageItem>.Filter;

    public ImageRepository(MongoContext ctx) => _col = ctx.Images;

    private static FilterDefinition<ImageItem> NotDeleted => F.Eq(i => i.IsDeleted, false);

    public async Task<ImageItem?> GetByIdAsync(string id, bool includeDeleted = false, CancellationToken ct = default)
    {
        var filter = F.Eq(i => i.Id, id);
        if (!includeDeleted) filter &= NotDeleted;
        return await _col.Find(filter).FirstOrDefaultAsync(ct);
    }

    public async Task<PagedResult<ImageItem>> GetByFolderAsync(string folderId, ContentQuery query, CancellationToken ct = default)
    {
        var q = query.Normalized();
        var filter = F.Eq(i => i.FolderId, folderId) & NotDeleted;

        var sortBuilder = Builders<ImageItem>.Sort;
        var sort = (q.SortByDate, q.Descending) switch
        {
            (true, true) => sortBuilder.Descending(i => i.UploadedAt),
            (true, false) => sortBuilder.Ascending(i => i.UploadedAt),
            (false, true) => sortBuilder.Descending(i => i.Name),
            (false, false) => sortBuilder.Ascending(i => i.Name),
        };

        var total = await _col.CountDocumentsAsync(filter, cancellationToken: ct);
        var items = await _col.Find(filter)
            .Sort(sort)
            .Skip((q.Page - 1) * q.PageSize)
            .Limit(q.PageSize)
            .ToListAsync(ct);

        return new PagedResult<ImageItem>
        {
            Items = items,
            Total = total,
            Page = q.Page,
            PageSize = q.PageSize,
        };
    }

    public async Task<IReadOnlyDictionary<string, long>> CountByFolderIdsAsync(
        IReadOnlyCollection<string> folderIds, CancellationToken ct = default)
    {
        if (folderIds.Count == 0) return new Dictionary<string, long>();

        var filter = F.In(i => i.FolderId, folderIds) & NotDeleted;
        var grouped = await _col.Aggregate()
            .Match(filter)
            .Group(i => i.FolderId, g => new { FolderId = g.Key, Count = (long)g.Count() })
            .ToListAsync(ct);

        return grouped.ToDictionary(x => x.FolderId, x => x.Count);
    }

    public async Task<IReadOnlyList<ImageItem>> SearchByNameAsync(string query, int limit, CancellationToken ct = default)
    {
        var rx = new BsonRegularExpression(Regex.Escape(query), "i");
        var filter = F.Regex(i => i.Name, rx) & NotDeleted;
        return await _col.Find(filter).SortBy(i => i.Name).Limit(limit).ToListAsync(ct);
    }

    public Task InsertAsync(ImageItem image, CancellationToken ct = default)
        => _col.InsertOneAsync(image, cancellationToken: ct);

    public Task ReplaceAsync(ImageItem image, CancellationToken ct = default)
        => _col.ReplaceOneAsync(i => i.Id == image.Id, image, cancellationToken: ct);

    public async Task<bool> SoftDeleteAsync(string id, DateTime now, CancellationToken ct = default)
    {
        var update = Builders<ImageItem>.Update
            .Set(i => i.IsDeleted, true)
            .Set(i => i.UpdatedAt, now);
        var res = await _col.UpdateOneAsync(F.Eq(i => i.Id, id) & NotDeleted, update, cancellationToken: ct);
        return res.ModifiedCount > 0;
    }

    public async Task<long> SoftDeleteByFolderIdsAsync(
        IReadOnlyCollection<string> folderIds, DateTime now, CancellationToken ct = default)
    {
        if (folderIds.Count == 0) return 0;

        var update = Builders<ImageItem>.Update
            .Set(i => i.IsDeleted, true)
            .Set(i => i.UpdatedAt, now);
        var res = await _col.UpdateManyAsync(F.In(i => i.FolderId, folderIds) & NotDeleted, update, cancellationToken: ct);
        return res.ModifiedCount;
    }
}
