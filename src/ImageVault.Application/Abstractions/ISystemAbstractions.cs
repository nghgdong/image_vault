namespace ImageVault.Application.Abstractions;

/// <summary>Đồng hồ — tách ra để service thuần & test được. Luôn UTC.</summary>
public interface IClock
{
    DateTime UtcNow { get; }
}

/// <summary>Sinh Id mới (ObjectId dạng hex string). Cần để build materialized path trước khi insert.</summary>
public interface IIdGenerator
{
    string NewId();
}
