using ImageVault.Application.Abstractions;
using ImageVault.Application.Common;
using ImageVault.Application.DTOs;
using ImageVault.Domain.Entities;

namespace ImageVault.Application.Services;

/// <summary>
/// Truy vấn + quản lý ảnh (metadata). "Xóa" luôn là soft delete, KHÔNG gọi freeimage
/// xóa binary (SPEC §1.1). Upload xem các phương thức Upload* (Phase 2).
/// </summary>
public sealed class ImageService
{
    private readonly IImageRepository _images;
    private readonly IFolderRepository _folders;
    private readonly IClock _clock;

    public ImageService(IImageRepository images, IFolderRepository folders, IClock clock)
    {
        _images = images;
        _folders = folders;
        _clock = clock;
    }

    public async Task<ImageDetailDto> GetDetailAsync(string id, CancellationToken ct = default)
    {
        var img = await _images.GetByIdAsync(id, ct: ct)
                  ?? throw new NotFoundException($"Không tìm thấy ảnh '{id}'.");
        return ToDetail(img);
    }

    /// <summary>Soft-delete metadata 1 ảnh. Binary vẫn còn trên freeimage (SPEC §1.1).</summary>
    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        var ok = await _images.SoftDeleteAsync(id, _clock.UtcNow, ct);
        if (!ok) throw new NotFoundException($"Không tìm thấy ảnh '{id}'.");
    }

    /// <summary>Đổi tên và/hoặc di chuyển ảnh sang folder khác (SPEC §4.2 PUT /images/{id}).</summary>
    public async Task<ImageDetailDto> UpdateAsync(string id, UpdateImageRequest req, CancellationToken ct = default)
    {
        var img = await _images.GetByIdAsync(id, ct: ct)
                  ?? throw new NotFoundException($"Không tìm thấy ảnh '{id}'.");

        var changed = false;

        if (req.Name is not null)
        {
            var name = req.Name.Trim();
            if (name.Length == 0) throw new ValidationAppException("Tên ảnh không được để trống.");
            img.Name = name;
            changed = true;
        }

        if (req.FolderId is not null && req.FolderId != img.FolderId)
        {
            _ = await _folders.GetByIdAsync(req.FolderId, ct: ct)
                ?? throw new NotFoundException($"Không tìm thấy thư mục đích '{req.FolderId}'.");
            img.FolderId = req.FolderId;
            changed = true;
        }

        if (changed)
        {
            img.UpdatedAt = _clock.UtcNow;
            await _images.ReplaceAsync(img, ct);
        }

        return ToDetail(img);
    }

    private static ImageDetailDto ToDetail(ImageItem img) => new(
        img.Id, img.FolderId, img.Name, img.Url, img.ThumbUrl, img.MediumUrl,
        img.Width, img.Height, img.SizeBytes, img.MimeType, img.UploadedAt);
}
