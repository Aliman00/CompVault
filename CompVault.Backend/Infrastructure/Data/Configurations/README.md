# Infrastructure/Data/Configurations

> EF Core `IEntityTypeConfiguration<T>`-klasser som definerer kolonneoppsett, begrensninger, indekser og relasjoner. Holder `AppDbContext` ren.

## Struktur

Undermapper speiler `Domain/Entities/`-strukturen 1-til-1:

```
Configurations/
  Identity/        <- ApplicationUser, ApplicationRole, Department, Permission, RolePermission
  <Domene>/        <- ny undermappe per domeneomraade, speiler Domain/Entities/-strukturen
```

## Automatisk oppdaging

`AppDbContext` plukker opp alle konfigurasjoner automatisk via:

```csharp
builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
```

Du trenger **ikke** registrere nye konfigurasjoner manuelt — opprett filen, og EF Core finner den.

## Ny entitet? Gjør slik

1. Opprett `<Domene>/<EntitetNavn>Configuration.cs` i riktig undermappe
2. Implementer `IEntityTypeConfiguration<EntitetNavn>`

```csharp
namespace CompVault.Backend.Infrastructure.Data.Configurations.Identity;

internal sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.FullName).HasMaxLength(200).IsRequired();
    }
}
```

## Regler

- Klassen skal alltid vaere `internal sealed`
- Ingen forretningslogikk — kun kolonneoppsett og relasjoner
- Bruk `HasConversion<string>()` paa enums slik at DB-verdier er lesbare
- Bruk `HasQueryFilter` for soft-delete-filtrering paa entiteter med `DeletedAt`
