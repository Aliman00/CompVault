using CompVault.Shared.Result;

namespace CompVault.Backend.Infrastructure.Data;

/// <summary>
/// Abstraherer transaksjonsansvaret slik at service-laget kan samle flere
/// repository-operasjoner i én atomisk enhet før de persisteres til databasen.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// For vanlig, ikke-generisk Result-metoder:
    /// Oppretter en transaksjon i databasen, og lar oss kjøre en eller flere operasjoner innenfor denne.
    /// Rulles tilbake hvis noe går galt (feks feil under lagring i database,
    /// exception-kalles, early return).
    /// Brukes hvis flere operasjoner utfører database-handlinger, og vi ønsker ikke inkonsistent databasetilstand
    /// hvor operasjoner lykkes, mens andre feiler
    /// </summary>
    /// <param name="operation">En asynkron funksjon som inneholder en eller flere operasjoner</param>
    /// <param name="ct"></param>
    /// <returns>Result med Success eller Failure</returns>
    Task<Result> ExecuteInTransactionAsync(Func<Task<Result>> operation,
        CancellationToken ct = default);
    
    /// <summary>
    /// For generiske ResultT som returnerer verdier:
    /// Oppretter en transaksjon i databasen, og lar oss kjøre en eller flere operasjoner innenfor denne.
    /// Rulles tilbake hvis noe går galt
    /// For vanlig, ikke-generisk Result-metoder
    /// Brukes hvis flere operasjoner utfører database-handlinger, og vi ønsker ikke inkonsistent databasetilstand
    /// hvor operasjoner lykkes, mens andre feiler
    /// </summary>
    /// <param name="operation">En asynkron funksjon som inneholder en eller flere operasjoner
    /// (f.eks. kall til andre servicers og andre metoder, eller forretningslogikk)</param>
    /// <param name="ct"></param>
    /// <typeparam name="T">Typen på verdien til Result-objektet (f.eks. LoginResponse)</typeparam>
    /// <returns>Generisk Result med Success, og objektet</returns>
    Task<Result<T>> ExecuteInTransactionAsync<T>(Func<Task<Result<T>>> operation,
        CancellationToken ct = default);
}
