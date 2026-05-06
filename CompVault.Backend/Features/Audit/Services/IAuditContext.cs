namespace CompVault.Backend.Features.Audit.Services;

/// <summary>
/// Scoped service som lar applikasjonskoden oppgi forretningskontekst
/// før et SaveChanges-kall. Interceptoren leser denne og legger
/// reason/actionOverride i AuditLog.Details.
/// </summary>
public interface IAuditContext
{
    /// <summary>Angi en årsak for handlingen, f.eks. "Sikkerhetsbrudd ved truckkjøring".</summary>
    void SetReason(string reason);

    /// <summary>
    /// Overstyr action-type, f.eks. "competency.revoke" i stedet for "competency.update".
    /// </summary>
    void SetActionOverride(string action);

    /// <summary>Årsak satt av service-kode.</summary>
    string? Reason { get; }

    /// <summary>Action-override satt av service-kode.</summary>
    string? ActionOverride { get; }

    /// <summary>Tøm konteksten etter at interceptoren har lest den.</summary>
    void Clear();
}