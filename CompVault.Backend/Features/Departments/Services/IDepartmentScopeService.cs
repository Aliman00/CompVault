namespace CompVault.Backend.Features.Departments.Services;

/// <summary>
/// Vi henter brukerens tilatte avdelinger pr http-forespørsel.
/// Brukes av AppDbContext sine global query filters og SaveChangesInterceptor
/// </summary>
public interface IDepartmentScopeService
{
    /// <summary>
    /// Sjekker om brukeren har bypass i tilattelser til å se alle avdelinger (over og under)
    /// </summary>
    bool HasBypass(string readAllPermission);

    /// <summary>
    /// Henter alle avdelings-IDer brukeren har lov til å se entiteter ifra. Henter default egen avdeling,
    /// men med readSubPermission så henter vi alle underavdelinger.
    /// Returnerer tom liste hvis brukeren mangler avdeling eller ikke er innlogget, for endepuntker med AllowAnonymous.
    /// </summary>
    IReadOnlyList<Guid> GetAllowedDepartmentIds(string? readSubPermission = null);

    /// <summary>
    /// Sjekker om brukeren har tilattelse til en spesifikk avdeling og eventuelt hva brukeren har tilattelse til.
    /// Sjekker begge typer tilattelser og brukeren sin egen avdeling.
    /// Brukes av SaveChangesInterceptor på inserts og updates
    /// </summary>
    bool IsAllowed(Guid departmentId, string readAllPermission, string? readSubPermission = null);
}