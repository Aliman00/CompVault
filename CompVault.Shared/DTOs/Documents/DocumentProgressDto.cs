namespace CompVault.Shared.DTOs.Documents;

/// <summary>
/// Fremdriftsstatistikk for en dokumenttype for en spesifikk bruker.
/// Viser hvor mange dokumenter brukeren har signert vs. totalt antall som krever signering.
/// </summary>
public sealed class DocumentProgressDto
{
    /// <summary>Totalt antall aktive dokumenter synlige for brukeren i denne dokumenttypen.</summary>
    public int Total { get; set; }

    /// <summary>Antall dokumenter brukeren har signert (gjeldende versjon).</summary>
    public int Signed { get; set; }

    /// <summary>Antall dokumenter som venter på signering.</summary>
    public int Pending { get; set; }

    /// <summary>Prosent fullført (0-100). 100 hvis ingen dokumenter krever signering.</summary>
    public int PercentComplete { get; set; }
}
