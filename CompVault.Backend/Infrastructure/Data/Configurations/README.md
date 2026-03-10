# Infrastructure/Data/Configurations

EF Core `IEntityTypeConfiguration<T>`-klasser som definerer kolonneoppsett, begrensninger, indekser og relasjoner for hver entitet. Holder `AppDbContext` ren — ingen konfigurasjonslogikk skal ligge der direkte.

`AppDbContext` plukker opp alle konfigurasjoner automatisk via:
```csharp
builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
```
Det betyr at du **ikke** trenger å registrere nye konfigurasjoner manuelt — bare opprett filen i riktig undermappe.

---

## Struktur

Undermapper speiler `Domain/Entities/`-strukturen 1-til-1:

```
Configurations/
  Identity/      ← ApplicationUser, ApplicationRole, Department, Permission, RolePermission
  Competencies/  ← opprettes i fase 4
  Documents/     ← opprettes i fase 5
  Equipment/     ← opprettes i fase 6
  ...
```

---

## Ny entitet? Gjør slik

1. Opprett `<Domene>/<EntitetNavn>Configuration.cs` i riktig undermappe
2. Implementer `IEntityTypeConfiguration<EntitetNavn>`
3. Det er alt — `ApplyConfigurationsFromAssembly` finner den automatisk

```csharp
namespace CompVault.Backend.Infrastructure.Data.Configurations.Identity;

internal sealed class MyEntityConfiguration : IEntityTypeConfiguration<MyEntity>
{
    public void Configure(EntityTypeBuilder<MyEntity> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        // ...
    }
}
```

---

## Regler

- Klassen skal være `internal sealed` — den brukes kun av EF Core internt
- Ingen forretningslogikk her, kun kolonneoppsett og relasjoner
- Bruk `HasConversion<string>()` på enums så DB-verdiene er lesbare uten å slå opp i kode
- Bruk `HasQueryFilter` for soft-delete-filtrering på entiteter som har `DeletedAt`
