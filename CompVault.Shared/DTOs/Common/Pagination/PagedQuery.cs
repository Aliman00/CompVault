namespace CompVault.Shared.DTOs.Common.Pagination;

/// <summary>
/// Felles paginerings-parametere som bindes fra query string.
/// Eksempel: ?page=1&amp;pageSize=20
/// Arv fra denne klassen for endepunkter som trenger både paginering og filtre.
/// </summary>
public record PagedQuery
{
    private const int MaxPageSize = 100;

    private int _page = 1;
    public int Page
    {
        get => _page;
        init => _page = Math.Max(value, 1);
    }

    private int _pageSize = 20;
    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = Math.Clamp(value, 1, MaxPageSize);
    }

    public int Skip => (Page - 1) * PageSize;
}