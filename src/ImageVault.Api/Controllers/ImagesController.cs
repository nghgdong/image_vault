using ImageVault.Application.Common;
using ImageVault.Application.DTOs;
using ImageVault.Application.Services;
using ImageVault.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ImageVault.Api.Controllers;

[ApiController]
[Route("api/images")]
public sealed class ImagesController : ControllerBase
{
    private readonly ImageService _images;

    public ImagesController(ImageService images) => _images = images;

    /// <summary>Chi tiết 1 ảnh cho lightbox (SPEC §4.1).</summary>
    [HttpGet("{id}")]
    public Task<ImageDetailDto> GetById(string id, CancellationToken ct) => _images.GetDetailAsync(id, ct);

    /// <summary>Soft-delete metadata (SPEC §4.2). Binary freeimage KHÔNG bị xóa (§1.1).</summary>
    [Authorize(Roles = Roles.Admin)]
    [HttpDelete("{id}")]
    public async Task<ActionResult<object>> Delete(string id, CancellationToken ct)
    {
        await _images.DeleteAsync(id, ct);
        return Ok(new { note = "Binary trên freeimage.host KHÔNG bị xóa (SPEC §1.1) — chỉ ẩn metadata." });
    }

    /// <summary>Đổi tên / di chuyển ảnh (SPEC §4.2).</summary>
    [Authorize(Roles = Roles.Admin)]
    [HttpPut("{id}")]
    public Task<ImageDetailDto> Update(string id, [FromBody] UpdateImageRequest req, CancellationToken ct)
        => _images.UpdateAsync(id, req, ct);

    /// <summary>Upload 1 ảnh (multipart) — qua backend, key freeimage không lộ ra FE (SPEC §4.2, §5).</summary>
    [Authorize(Roles = Roles.Admin)]
    [EnableRateLimiting("upload")]
    [DisableRequestSizeLimit]
    [HttpPost("upload")]
    public async Task<ActionResult<ImageDetailDto>> Upload(
        [FromForm] string folderId, IFormFile? file, [FromForm] string? name, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            throw new ValidationAppException("Thiếu file upload.");
        if (file.Length > ImageFileValidator.MaxBytes)
            throw new ValidationAppException($"File '{file.FileName}' vượt quá 64MB.");

        await using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        ms.Position = 0;

        var dto = await _images.UploadAsync(folderId, new UploadFile(file.FileName, ms, ms.Length), name, ct);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    /// <summary>Upload NHIỀU ảnh tuần tự → 207 Multi-Status (SPEC §4.2, §5.4).</summary>
    [Authorize(Roles = Roles.Admin)]
    [EnableRateLimiting("upload")]
    [DisableRequestSizeLimit]
    [HttpPost("upload-batch")]
    public async Task<IActionResult> UploadBatch(
        [FromForm] string folderId, [FromForm] List<IFormFile> files, CancellationToken ct)
    {
        if (files is null || files.Count == 0)
            throw new ValidationAppException("Không có file nào được gửi.");
        if (files.Count > ImageFileValidator.MaxBatchFiles)
            throw new ValidationAppException($"Tối đa {ImageFileValidator.MaxBatchFiles} file mỗi lần.");

        var streams = new List<MemoryStream>(files.Count);
        try
        {
            var uploads = new List<UploadFile>(files.Count);
            foreach (var f in files)
            {
                var ms = new MemoryStream();
                await f.CopyToAsync(ms, ct);
                ms.Position = 0;
                streams.Add(ms);
                uploads.Add(new UploadFile(f.FileName, ms, ms.Length));
            }

            var resp = await _images.UploadBatchAsync(folderId, uploads, ct);
            return StatusCode(StatusCodes.Status207MultiStatus, resp);
        }
        finally
        {
            foreach (var s in streams) await s.DisposeAsync();
        }
    }
}
