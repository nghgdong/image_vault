using ImageVault.Application.DTOs;
using ImageVault.Application.Services;
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
}
