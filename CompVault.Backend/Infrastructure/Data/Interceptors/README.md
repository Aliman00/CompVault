# Data / Interceptors

Her ligger EF Core-interceptorene. I CompVault har vi én: `AuditSaveChangesInterceptor`, som er grunnlaget for hele revisjonsloggen.

## Hva er en EF Core-interceptor?

Kort fortalt: en klasse som henger seg på i livssyklusen til databasen. `AuditSaveChangesInterceptor` kjører **før** EF Core sender SQL til databasen, leser `ChangeTracker` (som vet eksakt hva som har endret seg), bygger revisjons-entries og legger dem til i samme `SaveChanges`-kall. Resultatet er at databaseendringene og audit-loggingen skjer atomisk — alt lagres i én transaksjon.

Trengs revisjonslogging i en helt ny feature? Du trenger ikke gjøre noe — interceptor-filen ligger her og fanger alt så lenge du kaller `SaveChangesAsync()`.

## Hva logges automatisk

| Type endring | Detalj |
|---|---|
| **Opprettelse** | En entitet lagres for første gang → `entity.create` |
| **Endring** | En eksisterende entitet endres → `entity.update` med `changed_fields` |
| **Soft-delete** | `DeletedAt` endres fra null til en verdi → fanges og logges som `entity.delete` |
| **Hard-delete** | `Remove()` kalles → gamle verdier logges som `entity.delete` |
| **Action override** | `IAuditContext` lar en service overstyre action (f.eks. `competency.revoke` i stedet for `competency.update`) |

Ikke alt logges. Entiteter som er rent teknisk støy — for eksempel `OtpCode`, `RefreshToken`, `DocumentVersion` — hoppes over for å holde loggen ren.

## Når det IKKE logges automatisk

Bakgrunnsjobber som bruker `ExecuteUpdateAsync` — for eksempel `CompetencyStatusJob` — går utenom `ChangeTracker` og triggrer dermed ikke interceptoren. Disse må logge manuelt på egen hånd.

## Registrering

Interceptoren settes opp i `Infrastructure/Extensions/ServiceCollectionExtensions.cs` med tilgang til `IServiceProvider`, slik at den kan hente `IAuditContext` og `IHttpContextAccessor` per request:

```csharp
options.AddInterceptors(new AuditSaveChangesInterceptor(sp));
```

For å se full implementasjon av interceptoren, se koden i `AuditSaveChangesInterceptor.cs`.
