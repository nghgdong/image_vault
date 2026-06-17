using ImageVault.Application.DTOs;
using ImageVault.Application.Services;
using ImageVault.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
}
