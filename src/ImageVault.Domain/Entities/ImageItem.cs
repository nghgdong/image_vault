namespace ImageVault.Domain.Entities;

/// <summary>
/// Metadata 1 ảnh. Binary thật nằm trên freeimage.host (public vĩnh viễn) — SPEC §3.2, §1.1.
/// "Xóa" chỉ là soft delete metadata; KHÔNG gọi freeimage để xóa binary.
/// </summary>
public class ImageItem
{
    public string Id { get; set; } = default!;

    /// <summary>Thư mục chứa ảnh.</summary>
    public string FolderId { get; set; } = default!;

    /// <summary>Tên hiển thị (mặc định = tên file gốc).</summary>
    public string Name { get; set; } = default!;

    /// <summary>image.url từ freeimage (full size).</summary>
    public string Url { get; set; } = default!;

    /// <summary>image.thumb.url (fallback = url nếu thiếu).</summary>
    public string ThumbUrl { get; set; } = default!;

    /// <summary>image.medium.url nếu có.</summary>
    public string? MediumUrl { get; set; }

    public int? Width { get; set; }
    public int? Height { get; set; }
    public long? SizeBytes { get; set; }
    public string? MimeType { get; set; }

    /// <summary>image.id_encoded — lưu để tham chiếu. KHÔNG xóa được binary qua API (SPEC §1.1).</summary>
    public string? FreeImageId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime UploadedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
