namespace CompVault.Shared.DTOs.Common.Pagination;

/// <summary>
/// Standardisert paginert respons med metadata om totalt antall sider.
/// Pagineringen (Skip/Take) SKAL gjøres på IQueryable i service/repository,
/// IKKE in-memory etter ToListAsync().
/// </summary>
public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;

    public static PagedResult<T> Create(IReadOnlyList<T> items, int totalCount, PagedQuery query) =>
        new()
        {
            Items = items,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
}