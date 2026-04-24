using CompVault.Shared.DTOs.Common.Pagination;
using CompVault.Shared.Enums;

namespace CompVault.Shared.DTOs.Documents;

/// <summary>
/// Query-parametere for filtrering, sortering og paginering av dokumenter for en bruker
/// </summary>
public sealed record DocumentQueryParameters : PagedQuery
{
    public Guid? UserId { get; init; }
    public DocumentSignatureFilter SignatureFilter { get; init; } = DocumentSignatureFilter.All;
    public DocumentSortField SortBy { get; init; } = DocumentSortField.UploadedAt;
    public bool SortDescending { get; init; } = true;
}
