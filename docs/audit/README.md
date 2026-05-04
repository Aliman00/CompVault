# Revisjonsloggen (Audit) — Moduldokumentasjon

Audit-modulen er CompVault sitt revisjonsspor — alle vesentlige endringer i systemet logges automatisk med hvem, hva, når og hvorfor. Dette er påkrevd for å tilfredsstille dokumentasjonskrav fra blant annet Arbeidstilsynet, der alt fra kompetansebevis til utstyrsutleveringer må kunne spores i ettertid.

## 1. Problemstilling og behov

Utgangspunktet for modulen var:
> Hvordan kan vi sikre at alle endringer i systemet blir logget automatisk, med nok detaljer til å kunne spore hva som skjedde — uten at utviklere må huske på å logge manuelt?

Konkrete krav til løsningen:
- All opprettelse, endring og sletting av forretningsdata skal logges automatisk.
- Hver loggoppføring må inneholde hvem som gjorde det (bruker), hva som ble gjort (action), hvilken entitet det gjaldt, og når det skjedde.
- Endringer må kunne spores på tvers av soft-delete — revisjonsloggen må være uavhengig av om brukeren senere deaktiveres.
- Loggingen må skje atomisk med selve databaseendringen — hvis en endring feiler, skal det ikke ligge igjen en revisjonsoppføring uten at selve endringen også skjedde.
- Administratorer må kunne søke og filtrere i loggen på action, entitetstype, entitet, bruker og tidsrom.
- Tjenestekoden må kunne gi ekstra kontekst ved spesielle handlinger (f.eks. tilbakekalling av kompetanse — da skal action overstyres til `competency.revoke` og en årsak legges ved).

## 2. Teknisk design

### Datamodell

Kjernen er én enkelt entitet: `AuditLog`. Ingen FK-er, ingen soft-delete — loggen er permanent og selvforsynt. Den fullstendige datamodellen er dokumentert i `audit-er-diagram.pdf`.

**AuditLog:**
| Felt | Type | Hva det er |
|------|------|------------|
| `Id` | Guid | Primærnøkkel |
| `Action` | varchar(100) | Handlingstype, f.eks. `competency.revoke`, `document.create` |
| `EntityType` | varchar(50) | Entitetstype som ble endret, f.eks. `Competency`, `Document` |
| `EntityId` | Guid | ID til entiteten som ble endret |
| `UserId` | Guid? | ID til brukeren som utførte handlingen. Null for bakgrunnsjobber. |
| `UserEmail` | varchar(256) | Denormalisert e-post — overlever at brukeren deaktiveres |
| `UserName` | varchar(200) | Denormalisert navn — overlever at brukeren deaktiveres |
| `Details` | jsonb | Fleksible detaljer per action-type. F.eks. `changed_fields`, `revoked_reason`, `old_version`/`new_version` |
| `CreatedAt` | DateTime | Når handlingen ble utført (UTC) |

**Indekser:**
- `Action` — filtrering på handlingstype
- `(EntityType, EntityId)` — historikk per entitet
- `UserId` — filtrering på bruker
- `CreatedAt` (descending) — tidsbasert sortering
- `(EntityType, EntityId, CreatedAt)` (desc) — den vanligste spørringen: vis alle endringer på én entitet, nyeste først

### Arkitektur

Loggingen er todelt. Selve skrivingen til revisjonsloggen skjer automatisk i `AuditSaveChangesInterceptor` — en EF Core-interceptor som henger seg på `SaveChanges`-pipeline-en. Lesing og visning av loggen går gjennom `AuditController` → `AuditLogService` → rett mot databasen (ingen eget repository, spørringene er enkle nok til at de ligger i servicen). Samspillet er vist i `audit-arkitektur.png`.

**Komponentoversikt:**

| Komponent | Type | Ansvar |
|-----------|------|--------|
| `AuditController` | Controller | GET `/api/audit-log` med filtrering og paginering. `audit:read`. |
| `AuditLogService` | Service | Bygger opp IQueryable med valgfrie filtre (Action, EntityType, EntityId, UserId, From/To). Paginering + sortering nyeste først. |
| `AuditLogMapper` | Statisk klasse | `AuditLog → AuditLogDto`. Deserialiserer `Details` JSONB til `JsonElement`. |
| `AuditSaveChangesInterceptor` | EF Core Interceptor | Leser ChangeTracker før `SaveChanges`, bygger `AuditLog`-entries for Added/Modified/Deleted entiteter. Skriver i samme transaksjon. Hopper over teknisk støy (tokens, join-tabeller). |
| `IAuditContext` / `AuditContext` | Scoped Service | Lar tjenestekoden sette `Reason` og `ActionOverride` før `SaveChanges`. Interceptoren leser konteksten og legger det i `Details`. Tømmes etter hvert kall. |

### Designvalg

| Valg | Hvorfor |
|------|--------|
| **Ingen FK til ApplicationUser** | Hvis brukeren slettes (soft-delete), må revisjonsloggen fortsatt være lesbar. `UserEmail` og `UserName` er denormalisert — historikken består uansett. |
| **JSONB for Details** | Hver action-type kan ha helt ulike detaljer (revoked_reason for kompetanse, changed_fields for brukeroppdatering). JSONB er fleksibelt uten schema-endringer, og PostgreSQL indekserer det. |
| **Interceptor, ikke manuell logging** | Utviklere skal ikke måtte huske på å logge. Interceptoren fanger alt automatisk via ChangeTracker. Unntaket er `ExecuteUpdateAsync` (bulk-operasjoner) som går utenom — der logger `CompetencyStatusJob` manuelt. |
| **IAuditContext for presisjon** | Automatisk logging gir generiske action-navn (`competency.update`). Ved tilbakekalling ønsker vi `competency.revoke` med årsak. `IAuditContext` lar servicen gi denne presise konteksten uten å måtte logge manuelt. |
| **Atomisk logging** | `AuditSaveChangesInterceptor` legger revisjonsoppføringer i samme `DbContext` før `SaveChanges`. Alt lagres i én transaksjon — umulig å få revisjonsoppføring uten databaseendring, eller omvendt. |
| **Scoped AuditContext** | `AuditContext` lever per HTTP-request og tømmes etter hver `SaveChanges`. Hindrer at kontekst fra et tidligere kall "blør over" til et senere kall i samme request. |
| **Hopp over teknisk støy** | `OtpCode`, `RefreshToken`, `AspNetUserRoles`, `RolePermission`, og selve `AuditLog` logges ikke. Dette er intern infrastruktur som bare ville laget støy i revisjonsloggen. |

## 3. Implementasjon

`AuditSaveChangesInterceptor` er den sentrale komponenten — det er her all magien skjer. Den implementerer `ISaveChangesInterceptor` og overstyrer `SavingChangesAsync`. Før EF Core sender SQL til databasen, leser interceptoren `ChangeTracker.Entries()` og itererer over alle endrede entiteter. For hver entitet sjekker den om typen skal logges (hopper over `OtpCode`, `RefreshToken`, join-tabeller og `AuditLog` selv). Hvis ja, bygger den en `AuditLog`-entry:

- **Added** → `{entity}.create`
- **Modified** → `{entity}.update`
- **Deleted** → `{entity}.delete`

For Modified-entiteter inkluderer den `changed_fields` i `Details` — en liste over hvilke properties som faktisk ble endret, med gammel og ny verdi. Dette er spesielt nyttig for debugging og compliance.

Interceptoren bruker `IAuditContext` (hentet via `IServiceProvider`) for å sjekke om servicen har satt en `ActionOverride` eller `Reason`. Hvis `ActionOverride` er satt, overstyres den automatiske action-typen. `Reason` legges i `Details` under nøkkelen `reason`.

Når interceptoren er ferdig, er alle `AuditLog`-entiteter lagt til i `DbContext` sin ChangeTracker. Når `SaveChangesAsync` fullfører, skrives både forretningsendringene og revisjonsoppføringene i samme transaksjon.

`AuditLogService` er enkel og rett fram. `GetAsync` tar et `AuditLogQueryParameters`-objekt med valgfrie filtre og bygger opp en `IQueryable` med `Where`-klausuler for hvert filter som er satt. Deretter teller den totalt antall (for paginering), sorterer på `CreatedAt` synkende, og paginerer med `Skip`/`Take`. Resultatet mappes via `AuditLogMapper.ToDto` som deserialiserer `Details` JSONB til et `JsonElement` for fleksibel visning i frontend.

`AuditController` er en minimalistisk controller — én GET-endepunkt, `[Authorize(Policy = Permissions.AuditRead)]`, mottar query-parametre og returnerer et paginert resultat. Ingen POST, PUT eller DELETE — revisjonsloggen er ren lesing fra API-siden.

## 4. Utfordringer og beslutninger

### Automatisk logging vs. presisjon

Automatisk logging via ChangeTracker er fantastisk for 95 % av tilfellene — du får `document.create`, `user.update`, `department.delete` helt uten å tenke på det. Men noen handlinger trenger mer presisjon. Når en kompetanse tilbakekalles, er det teknisk sett en oppdatering av `Competency`-entiteten (status endres til `Revoked` + `RevokedReason` settes). Men fra et forretningsperspektiv er dette en helt annen handling enn en vanlig oppdatering — det er en `competency.revoke`.

Vi kunne bygget inn spesiallogikk i interceptoren for å detektere dette (sjekke om `Status` ble endret til `Revoked`), men det hadde gjort interceptoren skjør og vanskelig å utvide. I stedet introduserte vi `IAuditContext` — en tynn scoped service som lar servicen si "neste SaveChanges skal ha action `competency.revoke` og reason `Sikkerhetsbrudd ved truckkjøring`". Interceptoren leser dette og bruker det. Rent, enkelt, uten spesialkode.

### Denormalisert brukerinfo

Hvorfor ikke en FK til `ApplicationUser`? To grunner. Én: `ApplicationUser` har soft-delete og query-filter (`DeletedAt == null`). Hvis en bruker slettes, ville revisjonsloggen plutselig mistet tilknytningen — `UserId`-en ville ikke lenger matche noen rad i `ApplicationUser`-tabellen (fra EF Core sitt perspektiv, på grunn av query-filteret). To: vi ønsker at revisjonsloggen skal være kompromissløst permanent. Selv om en bruker fjernes fullstendig fra systemet, skal revisjonsloggen fortsatt vise "denne handlingen ble utført av ola.nordmann@lekestua.no".

Løsningen er denormalisering: `UserEmail` og `UserName` skrives direkte inn i `AuditLog` på loggingstidspunktet. `UserId` er fortsatt med som en Guid (uten FK) for filtrering, men navn og e-post er alltid tilgjengelig uavhengig av brukerens status.

### Bulk-operasjoner og manglende ChangeTracker

`CompetencyStatusJob` bruker `ExecuteUpdateAsync` for å bulk-oppdatere status på alle kompetansebevis som har utløpt eller nærmer seg utløp. Dette er raskt, men går helt utenom ChangeTracker — og dermed utenom `AuditSaveChangesInterceptor`. Ingen revisjonslogg blir skrevet automatisk.

Vi måtte velge: droppe `ExecuteUpdateAsync` og gå tilbake til ChangeTracker (tregere, men automatisk logging), eller beholde bulk-oppdateringen og logge manuelt. Vi valgte det siste. `CompetencyStatusJob` henter ut alle berørte bevis med gammel og ny status før bulk-oppdateringen, skriver én `AuditLog`-rad per endring manuelt, og kjører deretter `ExecuteUpdateAsync`. Det er mer kode, men det er verdt det for ytelsen når antall kompetansebevis vokser.

## 5. Vurdering og refleksjon

*(Denne seksjonen fylles ut senere.)*

## 6. Relaterte moduler

| Modul | Relasjon |
|-------|----------|
| **Competencies** | `CompetencyService` bruker `IAuditContext` for å overstyre action ved revoke; `CompetencyStatusJob` skriver manuell audit for bulk-oppdateringer |
| **RBAC** | Permission: `audit:read` — kun Admin har tilgang til revisjonsloggen |
| **Auth** | `AuditSaveChangesInterceptor` trenger autentisert bruker for `UserId`/`UserEmail`/`UserName` — hentes fra `IUserContext` |
| **Documents** | Alle dokument-operasjoner (create, update, delete, sign) logges automatisk via interceptoren |
| **Equipment** | Alle utstyrsoperasjoner (kategori, item, utlevering) logges automatisk via interceptoren |
| **Users** | Alle brukeroperasjoner logges automatisk; `UserName` og `UserEmail` denormaliseres inn i loggen |
| **Departments** | Alle avdelingsoperasjoner logges automatisk via interceptoren |
