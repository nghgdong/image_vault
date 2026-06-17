namespace ImageVault.Application.Common;

public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public long Total { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}

/// <summary>Tham số phân trang + sort cho nội dung thư mục (SPEC §4.1).</summary>
public sealed class ContentQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;

    /// <summary>"name" | "date".</summary>
    public string Sort { get; init; } = "name";

    /// <summary>"asc" | "desc".</summary>
    public string Order { get; init; } = "asc";

    public bool SortByDate => string.Equals(Sort, "date", StringComparison.OrdinalIgnoreCase);
    public bool Descending => string.Equals(Order, "desc", StringComparison.OrdinalIgnoreCase);

    public ContentQuery Normalized() => new()
    {
        Page = Page < 1 ? 1 : Page,
        PageSize = PageSize is < 1 or > 200 ? 50 : PageSize,
        Sort = Sort,
        Order = Order,
    };
}
