using CompVault.Backend.Domain.Entities.JobTitles;

namespace CompVault.Backend.Domain.Entities.Documents;

/// <summary>
/// Koblingstabell mellom dokument og jobbtittel for målgruppe.
/// Brukes når DocumentType.TargetMode er JobTitle.
/// </summary>
public class DocumentJobTitle
{
    /// <summary>ID til dokumentet.</summary>
    public Guid DocumentId { get; set; }

    /// <summary>ID til jobbtittelen.</summary>
    public Guid JobTitleId { get; set; }

    /// <summary>Navigasjon til dokumentet.</summary>
    public Document? Document { get; set; }

    /// <summary>Navigasjon til jobbtittelen.</summary>
    public JobTitle? JobTitle { get; set; }
}