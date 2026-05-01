# Infrastructure/Data

`Infrastructure/Data/` er stedet der databasekoden vår er samlet. Her ligger `AppDbContext`, EF Core-konfigurasjoner og `IUnitOfWork`.

## Struktur

```text
Infrastructure/Data/
├── AppDbContext.cs         <- Hoved-DbContext (arver fra IdentityDbContext)
├── AppDbContextFactory.cs  <- Design-time factory for EF Core-migrasjoner
├── IUnitOfWork.cs          <- Interface for transaksjonshåndtering
├── UnitOfWork.cs           <- Implementasjon med ExecuteInTransactionAsync
├── DatabaseSettings.cs     <- Konfigurasjon for database-tilkobling
├── Configurations/         <- EF Core-konfigurasjoner per entitet
│   └── <Domene>/
│       └── <Entity>Configuration.cs
└── Interceptors/           <- EF Core-interceptorer (AuditSaveChangesInterceptor)
```

## AppDbContext

`AppDbContext` er hovedinngangen mot databasen og inneholder alle relevante `DbSet`s. Konfigurasjonene ligger i egne filer under `Configurations/`, og blir plukket opp automatisk av EF Core via `ApplyConfigurationsFromAssembly`.

## IUnitOfWork

`IUnitOfWork` brukes for **transaksjoner** når flere operasjoner skal skje atomisk:

```csharp
public interface IUnitOfWork
{
    Task<Result> ExecuteInTransactionAsync(Func<Task<Result>> operation, CancellationToken ct);
    Task<Result<T>> ExecuteInTransactionAsync<T>(Func<Task<Result<T>>> operation, CancellationToken ct);
}
```

Service-laget bruker dette når flere repositories skal endre data sammen — da wrappes alt i én transaksjon som rulles tilbake hvis noe feiler.

## Vanlig lagring

For enkle operasjoner der bare én repository er involvert, kalles `SaveChangesAsync()` direkte via repository. Se `Infrastructure/Repositories/README.md`.

## Retningslinjer

- `AppDbContext` brukes i utgangspunktet her, mens andre deler av prosjektet går via repositories og services.
- `IUnitOfWork` brukes for transaksjoner; vanlig lagring skjer via repository.
- Nye entitetskonfigurasjoner legges i `Configurations/<Domene>/`, og EF Core finner dem automatisk.
