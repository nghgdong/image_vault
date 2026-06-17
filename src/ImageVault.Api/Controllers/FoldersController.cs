using System.Text.Json;
using ImageVault.Application.Common;
using ImageVault.Application.DTOs;
using ImageVault.Application.Services;
using ImageVault.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImageVault.Api.Controllers;

[ApiController]
[Route("api/folders")]
public sealed class FoldersController : ControllerBase
{
    private readonly FolderService _folders;

    public FoldersController(FolderService folders) => _folders = folders;

    // ---------- Public (SPEC §4.1) ----------

    /// <summary>Root ảo: trả các thư mục cấp cao nhất.</summary>
    [HttpGet("root")]
    public Task<FolderChildrenDto> GetRoot(CancellationToken ct) => _folders.GetRootAsync(ct);

    /// <summary>Nội dung 1 thư mục: folder con + ảnh (phân trang + sort).</summary>
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

    /// <summary>Breadcrumb từ root tới thư mục hiện tại.</summary>
    [HttpGet("{id}/breadcrumb")]
    public Task<IReadOnlyList<BreadcrumbItemDto>> GetBreadcrumb(string id, CancellationToken ct)
        => _folders.GetBreadcrumbAsync(id, ct);

    // ---------- Admin (SPEC §4.2) ----------

    /// <summary>Tạo thư mục (409 nếu trùng tên trong cùng parent).</summary>
    [Authorize(Roles = Roles.Admin)]
    [HttpPost]
    public async Task<ActionResult<FolderCreatedDto>> Create([FromBody] CreateFolderRequest req, CancellationToken ct)
    {
        var folder = await _folders.CreateFolderAsync(req.Name, req.ParentId, ct);
        var dto = new FolderCreatedDto(folder.Id, folder.Name, folder.ParentId, folder.Path);
        return CreatedAtAction(nameof(GetChildren), new { id = folder.Id }, dto);
    }

    /// <summary>
    /// Đổi tên và/hoặc di chuyển. Dùng JsonElement để phân biệt "không gửi field"
    /// với "gửi parentId: null" (move ra top-level).
    /// </summary>
    [Authorize(Roles = Roles.Admin)]
    [HttpPut("{id}")]
    public async Task<ActionResult<FolderCreatedDto>> Update(string id, [FromBody] JsonElement body, CancellationToken ct)
    {
        if (TryGetStringOrNull(body, "name", out var name))
        {
            if (name is null)
                throw new ValidationAppException("Tên thư mục không được null.");
            await _folders.RenameFolderAsync(id, name, ct);
        }

        if (TryGetStringOrNull(body, "parentId", out var parentId))
            await _folders.MoveFolderAsync(id, parentId, ct);

        var folder = await _folders.GetFolderAsync(id, ct);
        return Ok(new FolderCreatedDto(folder.Id, folder.Name, folder.ParentId, folder.Path));
    }

    /// <summary>Soft-delete đệ quy. cascade=false và còn nội dung → 409. Binary freeimage KHÔNG bị xóa.</summary>
    [Authorize(Roles = Roles.Admin)]
    [HttpDelete("{id}")]
    public async Task<ActionResult<object>> Delete(string id, [FromQuery] bool cascade = true, CancellationToken ct = default)
    {
        var result = await _folders.SoftDeleteFolderAsync(id, cascade, ct);
        return Ok(new
        {
            result.DeletedFolders,
            result.DeletedImages,
            note = "Binary trên freeimage.host KHÔNG bị xóa (SPEC §1.1) — chỉ ẩn metadata.",
        });
    }

    /// <summary>true nếu field có mặt trong body (giá trị có thể là null).</summary>
    private static bool TryGetStringOrNull(JsonElement body, string name, out string? value)
    {
        value = null;
        if (body.ValueKind != JsonValueKind.Object || !body.TryGetProperty(name, out var prop))
            return false;
        value = prop.ValueKind == JsonValueKind.Null ? null : prop.GetString();
        return true;
    }
}
