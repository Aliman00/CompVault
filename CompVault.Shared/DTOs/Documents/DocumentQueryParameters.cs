using CompVault.Shared.DTOs.Common.Pagination;

namespace CompVault.Shared.DTOs.Documents;

/// <summary>
/// Query-parametere for filtrering og paginering av dokumenter for innlogget bruker.
/// </summary>
public sealed record MyDocumentQueryParameters : PagedQuery
{
    /// <summary>Filtrer på signeringsstatus. Null = alle.</summary>
    public MyDocumentStatus? Status { get; init; }
}

/// <summary>
/// Signeringsstatus-filter for brukers dokumenter.
/// </summary>
public enum MyDocumentStatus
{
    Signed,
    Pending
}
