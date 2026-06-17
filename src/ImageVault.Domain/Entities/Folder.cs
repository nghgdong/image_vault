namespace ImageVault.Domain.Entities;

/// <summary>
/// Thư mục trong cây (materialized path) — SPEC §3.1.
/// Id được lưu dưới dạng ObjectId trong Mongo (map ở Infrastructure), ở Domain dùng string.
/// </summary>
public class Folder
{
    public string Id { get; set; } = default!;

    /// <summary>Tên hiển thị, vd "Du lịch 2024".</summary>
    public string Name { get; set; } = default!;

    /// <summary>null = thư mục cấp cao nhất (top-level / root ảo).</summary>
    public string? ParentId { get; set; }

    /// <summary>Materialized path, vd "/&lt;ownId&gt;/" hoặc "/&lt;parentId&gt;/&lt;ownId&gt;/" — luôn kết bằng "/".</summary>
    public string Path { get; set; } = default!;

    /// <summary>0 = top-level.</summary>
    public int Depth { get; set; }

    /// <summary>Soft delete (SPEC §1.2). Không bao giờ hard delete.</summary>
    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
