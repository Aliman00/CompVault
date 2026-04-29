# AuditSaveChangesInterceptor — Hvordan det fungerer

Dette er den viktigste komponenten i revisjonsloggen. Den fanger **automatisk** alle databasedendringer og gjør dem om til AuditLog-entries — uten at service-koden må gjøre noe annet enn å kalle `SaveChangesAsync()`.

## Hva er en EF Core Interceptor?

EF Core har et konsept kalt **interceptors** — klasser som "henger seg på" i livssyklusen til databasen. Tenk på det som en mellommann som lytter på alt som skjer.

```
Service-kode                  Interceptor                    Database
─────────────                  ──────────                    ───────
entity.Status = Expired  ──►                              
entity.DeletedAt = now    ──►                              
SaveChangesAsync()        ──►  SavingChangesAsync()  ──►    INSERT INTO AuditLogs
                                                    ──►    UPDATE Competencies
                                                    ──►    COMMIT
```

Interceptoren kjører **før** EF Core sender SQL til databasen. Den leser ChangeTracker (som vet eksakt hva som har endret seg), bygger AuditLog-entries, og legger dem til i samme `SaveChanges`-kall. Resultat: alt lagres i én og samme transaksjon.

## Flyten i detalj

```
┌─────────────────────────────────────────────────────────────┐
│  1. Service kaller SaveChangesAsync()                        │
├─────────────────────────────────────────────────────────────┤
│  2. EF Core kaller SavingChangesAsync() på interceptoren     │
├─────────────────────────────────────────────────────────────┤
│  3. Interceptoren gjør:                                      │
│     a) Hent IAuditContext (hvis finnes) — action override   │
│        og reason fra service-koden                          │
│     b) Hent IHttpContextAccessor — hvem er innlogget?       │
│     c) Gå gjennom ChangeTracker.Entries:                    │
│        - For hver Added → BuildCreateEntry()               │
│        - For hver Modified → BuildModifiedEntry()           │
│        - For hver Deleted → BuildDeletedEntry()             │
│        - Hopp over ignorerte entiteter (OtpCode, etc.)     │
│     d) Legg AuditLog-entries i context.Set<AuditLog>()      │
│     e) Tøm IAuditContext.Clear()                            │
├─────────────────────────────────────────────────────────────┤
│  4. EF Core lagrer alt i én transaksjon:                    │
│     INSERT INTO "AuditLogs" (...)                           │
│     UPDATE "Competencies" SET ...                           │
│     COMMIT                                                  │
└─────────────────────────────────────────────────────────────┘
```

## Hvordan ulike handlinger logges

### Opprettelse (Added)

Når en ny entitet lagres for første gang:

```csharp
// Service-kode:
var competency = new Competency { UserId = user.Id, Status = Valid, ... };
repository.AddAsync(competency);
await repository.SaveChangesAsync();

// Automatisk AuditLog:
{
    "action": "competency.create",
    "entityType": "Competency",
    "entityId": "guid",
    "userId": "innlogget-bruker-id",
    "userName": "Kari Nordmann",
    "details": { "Status": "Valid", "IssuedDate": "2026-04-22", ... }
}
```

### Endring (Modified)

Når en eksisterende entitet endres:

```csharp
// Service-kode:
competency.Status = CompetencyStatus.Revoked;
competency.RevokedReason = "Sikkerhetsbrudd";
await repository.SaveChangesAsync();

// Automatisk AuditLog:
{
    "action": "competency.update",
    "details": {
        "changed_fields": {
            "Status": { "old": "Valid", "new": "Revoked" },
            "RevokedReason": { "old": null, "new": "Sikkerhetsbrudd" }
        }
    }
}
```

### Tilbakekalling med IAuditContext (action override)

Når en service vil gi mer spesifikk kontekst:

```csharp
// Service-kode i CompetencyService.RevokeAsync:
auditContext.SetActionOverride("competency.revoke");
auditContext.SetReason("Sikkerhetsbrudd ved truckkjøring");
competency.Status = CompetencyStatus.Revoked;
competency.RevokedReason = "Sikkerhetsbrudd ved truckkjøring";
await repository.SaveChangesAsync();

// Automatisk AuditLog — merk action og reason:
{
    "action": "competency.revoke",              // ← overstyrt fra "competency.update"
    "details": {
        "changed_fields": { ... },
        "reason": "Sikkerhetsbrudd ved truckkjøring"  // ← fra IAuditContext
    }
}
```

### Soft-delete (Modified med DeletedAt)

Når en entitet markeres som slettet ved å sette `DeletedAt`:

```csharp
// Service-kode:
competency.DeletedAt = DateTime.UtcNow;
competency.IsActive = false;
await repository.SaveChangesAsync();

// Automatisk AuditLog — interceptoren oppdager soft-delete:
{
    "action": "competency.delete",        // ← ikke "competency.update"!
    "details": null                        // ← ingen changed_fields ved soft-delete
}
```

Hvordan vet interceptoren at det er en soft-delete? Den sjekker om `DeletedAt` har endret seg fra `null` til en verdi:

```
OriginalValue: null        CurrentValue: 2026-04-22T15:30:00Z
             ──────────►  Dette betyr: soft-delete!
```

### Hard-delete (Deleted)

Når en entitet fysisk slettes med `DbSet.Remove()`:

```csharp
// Service-kode (f.eks. slette en rolle):
context.Roles.Remove(role);
await context.SaveChangesAsync();

// Automatisk AuditLog:
{
    "action": "application_role.delete",
    "details": { "Name": "TestRole", "Description": "..." }  // ← opprinnelige verdier
}
```

### DocumentSignature hard-delete — spesialhåndtering

Når signaturer fjernes ved dokumentversjon-oppdatering, lagres ekstra kontekst:

```csharp
// DocumentVersioningService.UploadVersionAsync:
// 1. Document oppdateres (Modified) → "document.upload_version"
// 2. DocumentSignatures slettes (Deleted) → "document.signature_removed"
// 3. DocumentVersion opprettes (Added) → ignorert (i skip-listen)

// AuditLog for signatur-fjerning:
{
    "action": "document.signature_removed",
    "entityType": "DocumentSignature",
    "details": {
        "document_id": "guid",
        "removed_user_id": "guid",
        "signed_at": "2026-04-20T10:00:00Z",
        "old_version": 1,
        "document_title": "Brannverninstruks",    // ← fra ChangeTracker
        "new_version": 2                          // ← fra ChangeTracker
    }
}
```

## Hvem utførte handlingen?

Interceptoren løser brukeridentitet fra HTTP-context:

| Scenario                  | userId     | userName              | userEmail          |
|--------------------------|------------|-----------------------|--------------------|
| Innlogget bruker via API | GUID       | "Kari Nordmann"       | "kari@example.com" |
| Bakgrunnsjobb            | `null`     | "System"              | `null`             |
| Seed-data (oppstart)     | `null`     | "System"              | `null`             |

Brukerinfo er **denormalisert** — den lagres direkte i AuditLog. Hvis Kari bytter navn eller e-post, forblir de gamle verdiene i revisjonsloggen nøyaktig slik de var da handlingen skjedde.

## Ignorerte entiteter

Ikke alt skal logges. Disse entitetene hoppes over:

| Entitet             | Hvorfor ignorert?                                    |
|--------------------|-------------------------------------------------------|
| `OtpCode`          | Kortlevde engangskoder — ikke relevant revisjon      |
| `RefreshToken`     | Token-rotasjon skjer hyppig — støy i loggen          |
| `AuditLog`         | Selvreferanse unngås (logg om logg = ♾️)             |
| `DocumentDepartment`| Join-tabell — logges via `document.update`           |
| `DocumentJobTitle` | Join-tabell — logges via `document.update`           |
| `DocumentVersion`  | Intern versjonering — logges via `document.upload_version` |
| `RolePermission`   | Logges aggregert som `role.permissions_assigned`      |

## Egenskaper som hoppes over

I `changed_fields` inkluderes ikke:

| Egenskap       | Hvorfor?                                        |
|---------------|--------------------------------------------------|
| `Id`          | Alltid den samme — ikke relevant                 |
| `CreatedAt`   | Settes én gang — aldri endret                    |
| Primærnøkler | Allerede lagret som `entityId`                     |
| Fremmednøkler | Logges som `entityId` for relatert entitet        |
| Skyggeegenskaper| EF Core-interne felt                         |
| `DeletedAt`   | Logges via soft-delete-action, ikke som felt      |
| `IsActive`    | Logges via soft-delete-action, ikke som felt       |

## IAuditContext — manuell kontekst

`IAuditContext` er en **scoped** service (lever per HTTP-request) som lar service-koden gi ekstra kontekst før `SaveChangesAsync()`:

```csharp
public interface IAuditContext
{
    void SetReason(string reason);          // F.eks. "Sikkerhetsbrudd ved truckkjøring"
    void SetActionOverride(string action);   // F.eks. "competency.revoke"
    string? Reason { get; }
    string? ActionOverride { get; }
    void Clear();                           // Tømmes automatisk etter SaveChanges
}
```

### Når bruke IAuditContext?

| Situasjon                          | Hvorfor?                                           |
|-----------------------------------|-----------------------------------------------------|
| Tilbakekalling av kompetanse      | Standard action er `competency.update`, men bør være `competency.revoke` |
| Oppdatering av dokumentversjon    | Standard action er `document.update`, men bør være `document.upload_version` |
| Legge til årsak                   | `reason` feltet gir kontekst som ikke synes i changed_fields |

### Når **ikke** bruke IAuditContext?

- Vanlige opprettelser — `entity.create` er automatisk og korrekt
- Vanlige oppdateringer — `entity.update` med `changed_fields` er tilstrekkelig
- Soft-delete — interceptoren oppdager `DeletedAt`-endring automatisk

## Bakgrunnsjobber (uten HTTP-context)

`ExecuteUpdateAsync` går **utenom** ChangeTracker og triggrer **ikke** interceptoren. Derfor må `CompetencyStatusJob` manuelt opprette AuditLog-entries:

```csharp
// I CompetencyStatusJob:
var auditEntries = statusChanges.Select(change => new AuditLog
{
    Action = "competency.status_auto_update",
    EntityType = "Competency",
    EntityId = change.CompetencyId,
    UserId = null,          // Ingen innlogget bruker
    UserName = "System",     // Markør for bakgrunnsjobb
    Details = JsonSerializer.Serialize(new {
        old_status = change.OldStatus.ToString(),
        new_status = change.NewStatus.ToString(),
        trigger = "expiry_check_job"
    }),
}).ToList();

dbContext.AuditLogs.AddRange(auditEntries);
await dbContext.SaveChangesAsync();
```

## DI-registrering

Interceptoren registreres i `ServiceCollectionExtensions.AddDatabase()` med tilgang til `IServiceProvider`:

```csharp
services.AddDbContext<AppDbContext>((sp, options) =>
{
    options.UseNpgsql(connectionString, npgsql => ...);
    options.AddInterceptors(new AuditSaveChangesInterceptor(sp));
});
```

`IServiceProvider` brukes til å løse `IAuditContext` og `IHttpContextAccessor` per scope — ikke i konstruktøren, fordi DbContext er scoped og lever per request.

## Testing

Se `AuditSaveChangesInterceptorTests.cs` for enhetstester som dekker:
- Opprettelse → `entity.create`
- Endring → `entity.update` med `changed_fields`
- Soft-delete → `entity.delete`
- Hard-delete → `entity.delete` med opprinnelige verdier
- IAuditContext action override og reason
- Ignorerte entiteter (OtpCode, RefreshToken, etc.)
- DocumentSignature-spesialhåndtering
- Ingen HTTP-context → `userName = "System"`



Det korte svaret (det du foreslo, forbedret)
> Jeg undersøkte ulike måter å implementere revisjonslogging på, og ble pekt mot EF Core sin SaveChangesInterceptor. Ved å override SavingChangesAsync fanger vi alle databaseendringer automatisk — vi trenger ikke å huske å logge manuelt i hver eneste service-metode. AuditLog-entries legges til i samme SaveChanges-kall, så alt lagres atomisk i én transaksjon.

Det litt dypere svaret (hvis noen graver)

Det finnes hovedsakelig 3 måter å gjøre revisjonslogging på i .NET/EF Core:
Tilnærming	Hvordan
Manuell logging i hver service	Kalle _auditLogRepository.Add(...) i hver metode
MediatR/MediatR pipeline	Dispatch events etter hver kommando
EF Core Interceptor ✅	Override SavingChangesAsync

Jeg valgte interceptoren fordi:
1. Umulig å glemme — Hver gang noe skrives til databasen via SaveChangesAsync(), fanges det automatisk. Ingen risiko for at en utvikler glemmer å logge en endring.

2. Atomisk — AuditLog-entries legges til i samme SaveChanges-kall som den opprinnelige endringen. Hvis databasen er nede, rulles alt tilbake sammen. Ingen "halv-loggde" hendelser.

3. ChangeTracker gir oss alt gratis — EF Core vet allerede hvilke properties som har endret seg (OriginalValue vs CurrentValue), hvem som er soft-deletet (DeletedAt: null → verdi), og hva som er lagt til vs fjernet. Vi trenger ikke å skrive denne logikken selv.

4. IAuditContext for unntak — Noen ganger trenger vi mer kontekst enn det ChangeTracker gir (f.eks. "hvorfor" ble kompetansen tilbakekalt). Da bruker vi IAuditContext for å angi action og reason før SaveChangesAsync(). Interceptor Leser dette og tømmer det automatisk etterpå.

5. Bakgrunnsjobber — ExecuteUpdateAsync går utenom ChangeTracker, så interceptoren ikke fanges. Derfor logger vi manuelt i CompetencyStatusJob.
Så ja — si at du ble pekt mot interceptoren og at du vurderte alternativene og valgte den fordi den ga best dekning med minst risiko for menneskelige feil.