using System.Text.RegularExpressions;
using ImageVault.Application.Abstractions;
using ImageVault.Domain.Entities;
using ImageVault.Infrastructure.Persistence;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ImageVault.Infrastructure.Repositories;

public sealed class FolderRepository : IFolderRepository
{
    private readonly IMongoCollection<Folder> _col;
    private static readonly FilterDefinitionBuilder<Folder> F = Builders<Folder>.Filter;

    public FolderRepository(MongoContext ctx) => _col = ctx.Folders;

    private static FilterDefinition<Folder> NotDeleted => F.Eq(f => f.IsDeleted, false);

    public async Task<Folder?> GetByIdAsync(string id, bool includeDeleted = false, CancellationToken ct = default)
    {
        var filter = F.Eq(f => f.Id, id);
        if (!includeDeleted) filter &= NotDeleted;
        return await _col.Find(filter).FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<Folder>> GetChildrenAsync(string? parentId, CancellationToken ct = default)
    {
        var filter = F.Eq(f => f.ParentId, parentId) & NotDeleted;
        return await _col.Find(filter).SortBy(f => f.Name).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Folder>> GetManyByIdsAsync(IEnumerable<string> ids, CancellationToken ct = default)
    {
        var idList = ids.Distinct().ToArray();
        if (idList.Length == 0) return Array.Empty<Folder>();
        var filter = F.In(f => f.Id, idList) & NotDeleted;
        return await _col.Find(filter).ToListAsync(ct);
    }

    public async Task<bool> ExistsByNameInParentAsync(string? parentId, string name, string? excludeId, CancellationToken ct = default)
    {
        var filter = F.Eq(f => f.ParentId, parentId) & F.Eq(f => f.Name, name) & NotDeleted;
        if (excludeId is not null) filter &= F.Ne(f => f.Id, excludeId);
        return await _col.Find(filter).AnyAsync(ct);
    }

    public async Task<IReadOnlyDictionary<string, long>> CountChildrenByParentIdsAsync(
        IReadOnlyCollection<string> parentIds, CancellationToken ct = default)
    {
        if (parentIds.Count == 0) return new Dictionary<string, long>();

        var filter = F.In(f => f.ParentId, parentIds) & NotDeleted;
        var grouped = await _col.Aggregate()
            .Match(filter)
            .Group(f => f.ParentId, g => new { ParentId = g.Key, Count = (long)g.Count() })
            .ToListAsync(ct);

        return grouped
            .Where(x => x.ParentId is not null)
            .ToDictionary(x => x.ParentId!, x => x.Count);
    }

    public Task InsertAsync(Folder folder, CancellationToken ct = default)
        => _col.InsertOneAsync(folder, cancellationToken: ct);

    public Task ReplaceAsync(Folder folder, CancellationToken ct = default)
        => _col.ReplaceOneAsync(f => f.Id == folder.Id, folder, cancellationToken: ct);

    public async Task<long> RebasePathAsync(
        string oldPathPrefix, string newPathPrefix, int depthDelta, DateTime now, CancellationToken ct = default)
    {
        // Khớp chính nó + toàn bộ con cháu theo prefix path.
        var filter = F.Regex(f => f.Path, new BsonRegularExpression("^" + Regex.Escape(oldPathPrefix)));

        // Pipeline update: thay prefix path bằng newPathPrefix, giữ nguyên phần đuôi; cộng depthDelta.
        var oldLen = oldPathPrefix.Length;
        var pipeline = new[]
        {
            new BsonDocument("$set", new BsonDocument
            {
                {
                    "path", new BsonDocument("$concat", new BsonArray
                    {
                        newPathPrefix,
                        new BsonDocument("$substrCP", new BsonArray
                        {
                            "$path", oldLen, new BsonDocument("$strLenCP", "$path")
                        })
                    })
                },
                { "depth", new BsonDocument("$add", new BsonArray { "$depth", depthDelta }) },
                { "updatedAt", now }
            })
        };

        PipelineDefinition<Folder, Folder> pipelineDef = pipeline;
        var update = new PipelineUpdateDefinition<Folder>(pipelineDef);
        var res = await _col.UpdateManyAsync(filter, update, cancellationToken: ct);
        return res.ModifiedCount;
    }

    public async Task<IReadOnlyList<string>> SoftDeleteByPathPrefixAsync(
        string pathPrefix, DateTime now, CancellationToken ct = default)
    {
        var filter = F.Regex(f => f.Path, new BsonRegularExpression("^" + Regex.Escape(pathPrefix))) & NotDeleted;

        var ids = await _col.Find(filter).Project(f => f.Id).ToListAsync(ct);
        if (ids.Count == 0) return ids;

        var update = Builders<Folder>.Update
            .Set(f => f.IsDeleted, true)
            .Set(f => f.UpdatedAt, now);
        await _col.UpdateManyAsync(filter, update, cancellationToken: ct);
        return ids;
    }
}
