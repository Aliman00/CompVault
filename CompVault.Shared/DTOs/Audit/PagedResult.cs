namespace CompVault.Shared.DTOs.Audit;

/// <summary>
/// Paginert resultat for revisjonsloggen.
/// </summary>
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}