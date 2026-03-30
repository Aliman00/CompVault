namespace CompVault.Backend.Infrastructure.Auth;

/// <summary>
/// Provider for å hente informasjon om den nåværende brukeren i en HTTP-kontekst.
/// </summary>
public interface ICurrentUserProvider
{
    /// <summary>
    /// Henter ID-en til den nåværende autentiserte brukeren, eller null hvis ikke autentisert.
    /// </summary>
    Guid? GetCurrentUserId();
}
