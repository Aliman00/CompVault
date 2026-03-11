# Infrastructure/Repositories

> Generisk repository-base. Alle domenespesifikke repositories extender disse.

## Innhold

| Fil | Ansvar |
|---|---|
| `IRepository.cs` | Generisk base-interface med standard CRUD-operasjoner |
| `BaseRepository.cs` | Implementasjon av `IRepository<T>` — extender denne for alle repositories |

## Domenespesifikke repositories

Domenespesifikke repositories opprettes i `Infrastructure/Data/<Domene>/` — ikke her:

```
Infrastructure/Data/
  Identity/        <- IUserRepository, UserRepository
  <Domene>/        <- ny undermappe per domeneomraade som legges til
```

## Unit of Work-mønsteret

Repositories registrerer endringer i EF Cores change-tracker, men kaller aldri `SaveChangesAsync()`. Service-laget eier transaksjonen via `IUnitOfWork`:

```csharp
public class MinService(IMinRepository repo, IUnitOfWork unitOfWork) : IMinService
{
    public async Task OppdaterAsync(MinEntity entity, CancellationToken ct)
    {
        await repo.UpdateAsync(entity, ct);
        await unitOfWork.SaveChangesAsync(ct); // én atomisk lagring
    }
}
```

`IUnitOfWork` og `UnitOfWork` ligger i `Infrastructure/Data/`.

## Ny feature? Gjør slik

1. Opprett `Infrastructure/Data/<Domene>/I<Feature>Repository.cs` med domenespesifikke metoder
2. Opprett `Infrastructure/Data/<Domene>/<Feature>Repository.cs` og extend `BaseRepository<T>`
3. Registrer i `Infrastructure/Extensions/ServiceCollectionExtensions.cs`

```csharp
public interface IUserRepository : IRepository<ApplicationUser>
{
    Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken ct = default);
}

public class UserRepository : BaseRepository<ApplicationUser>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context) { }

    public async Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await DbSet.FirstOrDefaultAsync(u => u.Email == email && u.IsActive, ct);
}
```

## Regler

- Ingen forretningslogikk — kun datahenting og change-tracking
- Bruk `IQueryable` internt, men returner alltid materialiserte lister (`List<T>`) ut av metoden
- Alle metoder skal ha `CancellationToken ct = default`-parameter
- Kall aldri `SaveChangesAsync()` inne i en repository-metode
