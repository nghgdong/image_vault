using ImageVault.Application.Abstractions;
using ImageVault.Application.Common;
using ImageVault.Application.DTOs;

namespace ImageVault.Application.Services;

/// <summary>
/// Truy vấn ảnh. Phase 1 chỉ cần GET chi tiết (public). Upload & mutation ở Phase 2.
/// </summary>
public sealed class ImageService
{
    private readonly IImageRepository _images;

    public ImageService(IImageRepository images) => _images = images;

    public async Task<ImageDetailDto> GetDetailAsync(string id, CancellationToken ct = default)
    {
        var img = await _images.GetByIdAsync(id, ct: ct)
                  ?? throw new NotFoundException($"Không tìm thấy ảnh '{id}'.");

        return new ImageDetailDto(
            img.Id, img.FolderId, img.Name, img.Url, img.ThumbUrl, img.MediumUrl,
            img.Width, img.Height, img.SizeBytes, img.MimeType, img.UploadedAt);
    }
}
