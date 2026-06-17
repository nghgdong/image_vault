using ImageVault.Application.Abstractions;
using MongoDB.Bson;

namespace ImageVault.Infrastructure.System;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}

/// <summary>Sinh Id dạng ObjectId hex (24 ký tự) khớp với cách lưu trong Mongo.</summary>
public sealed class ObjectIdGenerator : IIdGenerator
{
    public string NewId() => ObjectId.GenerateNewId().ToString();
}
