using ImageVault.Application.Abstractions;
using ImageVault.Application.DTOs;

namespace ImageVault.Application.Services;

/// <summary>Tìm kiếm toàn kho theo tên (folder + ảnh), read-only — public.</summary>
public sealed class SearchService
{
    private const int DefaultLimit = 30;
    private const int MaxLimit = 100;

    private readonly IFolderRepository _folders;
    private readonly IImageRepository _images;

    public SearchService(IFolderRepository folders, IImageRepository images)
    {
        _folders = folders;
        _images = images;
    }

    public async Task<SearchResults> SearchAsync(string? query, int? limit, CancellationToken ct = default)
    {
        var q = (query ?? string.Empty).Trim();
        if (q.Length == 0)
            return new SearchResults(q, Array.Empty<FolderHit>(), Array.Empty<ImageHit>());

        var take = Math.Clamp(limit ?? DefaultLimit, 1, MaxLimit);

        var folders = await _folders.SearchByNameAsync(q, take, ct);
        var images = await _images.SearchByNameAsync(q, take, ct);

        return new SearchResults(
            q,
            folders.Select(f => new FolderHit(f.Id, f.Name, f.ParentId)).ToList(),
            images.Select(i => new ImageHit(i.Id, i.Name, i.FolderId, i.Url, i.ThumbUrl, i.Width, i.Height)).ToList());
    }
}
