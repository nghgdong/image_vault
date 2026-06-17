using ImageVault.Domain.Entities;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace ImageVault.Infrastructure.Persistence;

/// <summary>
/// Bọc IMongoDatabase + collections. Đăng ký convention camelCase + class map ánh xạ
/// string Id ↔ ObjectId (giữ Domain không phụ thuộc MongoDB).
/// </summary>
public sealed class MongoContext
{
    private static readonly object MapLock = new();
    private static bool _mapped;

    public IMongoDatabase Database { get; }
    public IMongoCollection<Folder> Folders { get; }
    public IMongoCollection<ImageItem> Images { get; }
    public IMongoCollection<User> Users { get; }

    public MongoContext(IOptions<MongoOptions> options)
    {
        RegisterConventionsAndMaps();

        var opt = options.Value;
        var client = new MongoClient(opt.ConnectionString);
        Database = client.GetDatabase(opt.Database);

        Folders = Database.GetCollection<Folder>("folders");
        Images = Database.GetCollection<ImageItem>("images");
        Users = Database.GetCollection<User>("users");
    }

    private static void RegisterConventionsAndMaps()
    {
        if (_mapped) return;
        lock (MapLock)
        {
            if (_mapped) return;

            ConventionRegistry.Register(
                "image-vault-conventions",
                new ConventionPack
                {
                    new CamelCaseElementNameConvention(),
                    new IgnoreExtraElementsConvention(true),
                },
                _ => true);

            if (!BsonClassMap.IsClassMapRegistered(typeof(Folder)))
                BsonClassMap.RegisterClassMap<Folder>(cm =>
                {
                    cm.AutoMap();
                    cm.MapIdMember(f => f.Id)
                      .SetSerializer(new StringSerializer(BsonType.ObjectId))
                      .SetIdGenerator(StringObjectIdGenerator.Instance);
                    cm.MapMember(f => f.ParentId).SetSerializer(new StringSerializer(BsonType.ObjectId));
                });

            if (!BsonClassMap.IsClassMapRegistered(typeof(ImageItem)))
                BsonClassMap.RegisterClassMap<ImageItem>(cm =>
                {
                    cm.AutoMap();
                    cm.MapIdMember(i => i.Id)
                      .SetSerializer(new StringSerializer(BsonType.ObjectId))
                      .SetIdGenerator(StringObjectIdGenerator.Instance);
                    cm.MapMember(i => i.FolderId).SetSerializer(new StringSerializer(BsonType.ObjectId));
                });

            if (!BsonClassMap.IsClassMapRegistered(typeof(User)))
                BsonClassMap.RegisterClassMap<User>(cm =>
                {
                    cm.AutoMap();
                    cm.MapIdMember(u => u.Id)
                      .SetSerializer(new StringSerializer(BsonType.ObjectId))
                      .SetIdGenerator(StringObjectIdGenerator.Instance);
                });

            _mapped = true;
        }
    }
}
