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
    private readonly IFreeImageClient _freeImage;
    private readonly IClock _clock;
    private readonly IIdGenerator _ids;

    public ImageService(
        IImageRepository images,
        IFolderRepository folders,
        IFreeImageClient freeImage,
        IClock clock,
        IIdGenerator ids)
    {
        _images = images;
        _folders = folders;
        _freeImage = freeImage;
        _clock = clock;
        _ids = ids;
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

    // ---------- Upload (SPEC §4.2, §5) ----------

    /// <summary>Upload 1 file: validate → freeimage → lưu metadata. Trả chi tiết ảnh.</summary>
    public async Task<ImageDetailDto> UploadAsync(string folderId, UploadFile file, string? displayName, CancellationToken ct = default)
    {
        await EnsureFolderAsync(folderId, ct);
        var img = await DoUploadAsync(folderId, file, displayName, ct);
        return ToDetail(img);
    }

    /// <summary>
    /// Upload nhiều file TUẦN TỰ (SPEC §5.4.4): 1 file lỗi KHÔNG chặn file khác.
    /// Trả tổng kết để controller phát 207 Multi-Status.
    /// </summary>
    public async Task<BatchUploadResponse> UploadBatchAsync(string folderId, IReadOnlyList<UploadFile> files, CancellationToken ct = default)
    {
        await EnsureFolderAsync(folderId, ct);

        if (files.Count == 0)
            throw new ValidationAppException("Không có file nào được gửi.");
        if (files.Count > ImageFileValidator.MaxBatchFiles)
            throw new ValidationAppException($"Tối đa {ImageFileValidator.MaxBatchFiles} file mỗi lần.");

        var results = new List<BatchUploadResultItem>(files.Count);
        int succeeded = 0, failed = 0;

        for (var i = 0; i < files.Count; i++) // TUẦN TỰ — không Task.WhenAll (tránh rate-limit)
        {
            var f = files[i];
            try
            {
                var img = await DoUploadAsync(folderId, f, displayName: null, ct);
                results.Add(new BatchUploadResultItem(i, f.FileName, "success", ToDetail(img), null));
                succeeded++;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) // 1 file lỗi không ném ra ngoài — ghi nhận rồi tiếp tục
            {
                results.Add(new BatchUploadResultItem(i, f.FileName, "error", null, ex.Message));
                failed++;
            }
        }

        return new BatchUploadResponse(files.Count, succeeded, failed, results);
    }

    private async Task EnsureFolderAsync(string folderId, CancellationToken ct)
    {
        _ = await _folders.GetByIdAsync(folderId, ct: ct)
            ?? throw new NotFoundException($"Không tìm thấy thư mục '{folderId}'.");
    }

    private async Task<ImageItem> DoUploadAsync(string folderId, UploadFile file, string? displayName, CancellationToken ct)
    {
        var v = ImageFileValidator.Validate(file);
        if (!v.Ok) throw new ValidationAppException(v.Error!);

        if (file.Content.CanSeek) file.Content.Position = 0;
        var up = await _freeImage.UploadAsync(file.Content, file.FileName, ct);

        var name = string.IsNullOrWhiteSpace(displayName) ? file.FileName : displayName.Trim();
        var now = _clock.UtcNow;
        var img = new ImageItem
        {
            Id = _ids.NewId(),
            FolderId = folderId,
            Name = name,
            Url = up.Url,
            ThumbUrl = string.IsNullOrEmpty(up.ThumbUrl) ? up.Url : up.ThumbUrl,
            MediumUrl = up.MediumUrl,
            Width = up.Width,
            Height = up.Height,
            SizeBytes = up.SizeBytes ?? file.Length,
            MimeType = up.MimeType ?? v.Mime,
            FreeImageId = up.FreeImageId,
            IsDeleted = false,
            UploadedAt = now,
            UpdatedAt = now,
        };

        await _images.InsertAsync(img, ct);
        return img;
    }

    private static ImageDetailDto ToDetail(ImageItem img) => new(
        img.Id, img.FolderId, img.Name, img.Url, img.ThumbUrl, img.MediumUrl,
        img.Width, img.Height, img.SizeBytes, img.MimeType, img.UploadedAt);
}
