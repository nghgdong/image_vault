using ImageVault.Application.Common;
using ImageVault.Application.DTOs;
using ImageVault.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace ImageVault.Api.Controllers;

[ApiController]
[Route("api/folders")]
public sealed class FoldersController : ControllerBase
{
    private readonly FolderService _folders;

    public FoldersController(FolderService folders) => _folders = folders;

    /// <summary>Root ảo: trả các thư mục cấp cao nhất (SPEC §4.1).</summary>
    [HttpGet("root")]
    public Task<FolderChildrenDto> GetRoot(CancellationToken ct) => _folders.GetRootAsync(ct);

    /// <summary>Nội dung 1 thư mục: folder con + ảnh (phân trang + sort) (SPEC §4.1).</summary>
    [HttpGet("{id}/children")]
    public Task<FolderChildrenDto> GetChildren(
        string id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string sort = "name",
        [FromQuery] string order = "asc",
        CancellationToken ct = default)
    {
        var query = new ContentQuery { Page = page, PageSize = pageSize, Sort = sort, Order = order };
        return _folders.GetChildrenAsync(id, query, ct);
    }

    /// <summary>Breadcrumb từ root tới thư mục hiện tại (SPEC §4.1).</summary>
    [HttpGet("{id}/breadcrumb")]
    public Task<IReadOnlyList<BreadcrumbItemDto>> GetBreadcrumb(string id, CancellationToken ct)
        => _folders.GetBreadcrumbAsync(id, ct);
}
