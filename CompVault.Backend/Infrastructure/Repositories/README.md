# Infrastructure/Data/Repositories

Konkrete implementasjoner av repository-interfacene. Her skjer alle direkte database-operasjoner via EF Core.

## Struktur

- `IRepository.cs` — generisk base-interface med standard CRUD-operasjoner
- `BaseRepository.cs` — generisk implementasjon av `IRepository<T>`, extender denne for alle repositories
- `Identity/` — repositories for Identity-domenet (IUserRepository, UserRepository)
- `Competencies/` — opprettes i fase 4
- (osv. per domeneområde i takt med fasene)

Domainespecifikke repositories gruppers i undermapper som speiler `Domain/Entities/`-strukturen.
`BaseRepository.cs` og `IRepository.cs` beholder rot-namespacet siden de er generiske og ikke domenespesifikke.

## Unit of Work

Prosjektet bruker **Unit of Work-mønsteret** for å gi service-laget eierskap over transaksjoner.

- Repositories **legger til, oppdaterer og sletter entiteter** i EF Cores change-tracker, men **kaller aldri `SaveChangesAsync()`**.
- Service-laget injiserer `IUnitOfWork` og kaller `SaveChangesAsync()` når alle operasjoner i en use-case er fullført.
- Dette sikrer **atomiske transaksjoner** — enten persisteres alt, eller ingenting.

```csharp
public class MinService(IMinRepository repo, IUnitOfWork unitOfWork) : IMinService
{
    public async Task OppdaterAsync(...)
    {
        await repo.UpdateAsync(entity, ct);
        // Eventuelt flere repo-kall her...
        await unitOfWork.SaveChangesAsync(ct); // Én atomisk lagring
    }
}
```

`IUnitOfWork` og `UnitOfWork` ligger i `Infrastructure/Data/`.

## Ny feature? Gjør slik:

1. Opprett `MinDomene/IMinFeatureRepository.cs` med feature-spesifikke metoder
2. Opprett `MinDomene/MinFeatureRepository.cs` som extender `BaseRepository<MinFeatureEntity>`
3. Registrer i `Infrastructure/Extensions/ServiceCollectionExtensions.cs`:
   
   ```csharp
   services.AddScoped<IMinFeatureRepository, MinFeatureRepository>();
    ```

4. Injiser `IUnitOfWork` i servicen som bruker repository-et


## Regler

 - Ingen forretningslogikk her — kun datahenting og change-tracking
 - Bruk IQueryable internt, men returner alltid materialiserte lister (List<T>) ut av metoden
 - Alle metoder skal være async med CancellationToken-parameter
 - **Ikke kall `SaveChangesAsync()` i repository-metoder** — la service-laget eie transaksjonen via `IUnitOfWork`


# Eksempel

```csharp
public interface IUserRepository : IRepository<ApplicationUser>
{
    Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<List<ApplicationUser>> GetByDepartmentAsync(Guid departmentId, CancellationToken ct = default);
}

public class UserRepository : BaseRepository<ApplicationUser>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context) { }

    public async Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await DbSet.FirstOrDefaultAsync(u => u.Email == email && u.IsActive, ct);
}
```


Den viktigste regelen er **«la service-laget eie transaksjonen»** via `IUnitOfWork` — aldri kall `SaveChangesAsync()` inne i en repository-metode.
