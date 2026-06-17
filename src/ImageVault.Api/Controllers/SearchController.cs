using ImageVault.Application.DTOs;
using ImageVault.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace ImageVault.Api.Controllers;

[ApiController]
[Route("api/search")]
public sealed class SearchController : ControllerBase
{
    private readonly SearchService _search;

    public SearchController(SearchService search) => _search = search;

    /// <summary>Tìm kiếm toàn kho theo tên (folder + ảnh). Public, read-only.</summary>
    [HttpGet]
    public Task<SearchResults> Search([FromQuery] string q, [FromQuery] int? limit, CancellationToken ct)
        => _search.SearchAsync(q, limit, ct);
}
