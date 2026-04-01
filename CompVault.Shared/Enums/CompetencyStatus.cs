namespace CompVault.Shared.Enums;

/// <summary>
/// Status for et kompetansebevis. Beregnes automatisk basert på utløpsdato
/// (ved opprettelse, oppdatering og av bakgrunnsjobb),
/// unntatt Revoked som kun settes manuelt.
/// </summary>
public enum CompetencyStatus
{
    /// <summary>Kompetansebeviset er gyldig.</summary>
    Valid,

    /// <summary>Kompetansebeviset utløper innen 90 dager.</summary>
    ExpiringSoon,

    /// <summary>Kompetansebeviset har utløpt.</summary>
    Expired,

    /// <summary>Kompetansebeviset er tilbakekalt (kun manuell handling).</summary>
    Revoked
}