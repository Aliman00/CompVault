namespace CompVault.Backend.Features.Audit.Services;

/// <summary>
/// Scoped implementasjon av <see cref="IAuditContext"/>.
/// Lever per HTTP-request og tømmes etterSaveChanges.
/// </summary>
public sealed class AuditContext : IAuditContext
{
    public string? Reason { get; private set; }
    public string? ActionOverride { get; private set; }

    public void SetReason(string reason) => Reason = reason;

    public void SetActionOverride(string action) => ActionOverride = action;

    public void Clear()
    {
        Reason = null;
        ActionOverride = null;
    }
}