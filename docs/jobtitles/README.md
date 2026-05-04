# Stillingstittelmodulen — Moduldokumentasjon

Stillingstittelmodulen er kanskje den aller enkleste modulen i hele CompVault. En stillingstittel er bare et navn med et par flagg — men den brukes på tvers av systemet, spesielt i dokumentmålgrupper, brukerprofiler og avdelingsledelse. Så selv om modulen er liten, er den viktigere enn den ser ut.

## 1. Problemstilling og behov

Utgangspunktet for modulen var:
> Hvordan kan systemet sikre konsistente stillingstitler på tvers av brukere og dokumentmålsetting, uten at hver bruker skriver inn sin egen tittel fritt?

Konkrete krav til løsningen:
- En sentral liste over godkjente stillingstitler som alle brukere velger fra.
- En stillingstittel skal kunne knyttes til brukere (én-til-mange).
- En stillingstittel skal kunne knyttes til dokumenter (mange-til-mange, for målgruppe).
- Navn må være unike blant aktive titler — to aktive stillingstitler kan ikke hete det samme.
- En stillingstittel kan markeres som leder-stilling (`IsLeader`), og dette brukes av avdelingsmodulen for å validere avdelingsledere.
- Soft delete — historikken består og relaterte data brytes ikke.

## 2. Teknisk design

### Datamodell

Kun én entitet: `JobTitle`. Den fullstendige datamodellen er dokumentert i `jobtitles-er-diagram.pdf`.

**JobTitle:**
| Felt | Type | Hva det er |
|------|------|------------|
| `Id` | Guid | Primærnøkkel |
| `Name` | varchar(100) | Navn, f.eks. "Systemutvikler" eller "Avdelingsleder" |
| `IsLeader` | bool | Om stillingstittel regnes som leder-rolle (default: false) |
| `IsActive` | bool | Om den er aktiv (default: true) |
| `CreatedAt` | DateTime | Når den ble opprettet |
| `DeletedAt` | DateTime? | Soft delete |

**Unikhet:**
- Unik indeks på `Name` med filteret `WHERE "DeletedAt" IS NULL`. En slettet tittel kan opprettes på nytt med samme navn, men to aktive kan ikke hete det samme.
- Ved opprettelse og oppdatering bruker `JobTitleService` `NameExistsAsync` (case-insensitive via `ToLower()`) for å sjekke navnekonflikt. I tillegg er det en `DbUpdateException`-catch som fanger opp race conditions.

**Relasjoner til andre moduler:**
- `ApplicationUser.JobTitleId` — FK til JobTitle, satt via brukerprofil.
- `DocumentJobTitle.JobTitleId` — koblingstabell, dokument → jobbtittel (mange-til-mange).
- `Department.ManagerId` valideres via `JobTitle.IsLeader` i `DepartmentService.IsValidManagerAsync`.

### Arkitektur

`JobTitleService` håndterer forretningslogikken, `JobTitleRepository` tar seg av databasekallene, og `JobTitleMapper` konverterer til DTO. Samspillet er vist i `jobtitles-arkitektur.png`.

**Komponentoversikt:**

| Komponent | Type | Ansvar |
|-----------|------|--------|
| `JobTitlesController` | Controller | CRUD. `job_titles:read/write/delete`. |
| `JobTitleService` | Service | Navneunikhet (case-insensitive), partial update, DbUpdateException-sikkerhetsnett. |
| `JobTitleMapper` | Statisk klasse | `JobTitle → JobTitleDto`: Id, Name, IsLeader, IsActive. |
| `JobTitleRepository` | Repository | `NameExistsAsync` (case-insensitive), `SoftDeleteAsync`. |

### Designvalg

| Valg | Hvorfor |
|------|--------|
| **Unik indeks med soft-delete-filter** | `WHERE "DeletedAt" IS NULL` — hindrer duplikate aktive titler, men tillater gjenbruk av navn etter sletting. |
| **Case-insensitive navnesjekk** | `ToLower()` i LINQ — "Systemutvikler" og "systemutvikler" er samme tittel. |
| **DbUpdateException som sikkerhetsnett** | Fanger opp race conditions som service-sjekken ikke klarer. To samtidige creates med samme navn → unique constraint i DB stopper den ene. |
| **IsLeader-flagget** | Brukes av `DepartmentService` for å validere avdelingsledere. Kun brukere med `IsLeader = true` kan settes som `Department.ManagerId`. |
| **Ingen relasjoner i DTO** | `JobTitleDto` har kun Id, Name, IsLeader og IsActive. Ingen bruker-liste eller dokument-liste — de hentes fra andre moduler. |

## 3. Implementasjon

`JobTitleService` er enkel men ryddig. `GetAllAsync` og `GetByIdAsync` er standard CRUD — ingenting overraskende der. `CreateAsync` trimmer navnet og sjekker mot `NameExistsAsync` før lagring. Hvis navnet finnes fra før, får klienten 409 Conflict. Etter lagring er det en `DbUpdateException`-catch — dette er sikkerhetsnettet for race conditions. Hvis to requests prøver å opprette "Systemutvikler" samtidig, vil den unike indeksen i databasen stoppe den siste.

`UpdateAsync` støtter partial update — `Name`, `IsActive` og `IsLeader` er alle nullable. Hvis `Name` endres, trimmes og sjekkes det mot `NameExistsAsync`. `IsActive` kan toggles av/på. `IsLeader`-endring er viktig fordi det påvirker hvem som kan være avdelingsleder. Også her er det `DbUpdateException`-sikkerhetsnett ved lagring.

`DeleteAsync` gjør soft delete — `DeletedAt = DateTime.UtcNow` og `IsActive = false`. Brukere som har denne stillingstittelen blir ikke påvirket — `ApplicationUser.JobTitleId` er `OnDelete: SetNull` så de mister bare tilknytningen.

`JobTitleRepository` bruker `ToLower()` i LINQ for case-insensitive navnesøk. `SoftDeleteAsync` følger standardmønsteret fra de andre modulene.

`JobTitleMapper` er under 20 linjer og mapper bare fire felt. Enkelt og oversiktlig.

## 4. Utfordringer og beslutninger

### Egentlig ingen store utfordringer

Dette er en av de modulene som "bare fungerte". Den har én entitet, standard CRUD, og ingen komplisert forretningslogikk. Det eneste som var verdt en diskusjon var hvordan vi skulle håndtere unike navn med soft delete.

### Navneunikhet med soft delete

Vi kunne lagt unikheten på `Name` uten filter, men da kunne man aldri gjenbruke et navn etter sletting. I stedet brukte vi en delvis unik indeks: `WHERE "DeletedAt" IS NULL`. Det betyr at "Systemutvikler" bare kan finnes én gang blant aktive titler, men kan dukke opp igjen etter å ha blitt slettet.

### Case-insensitive eller ikke?

Vi valgte case-insensitive. "Avdelingsleder" og "avdelingsleder" skal være samme tittel. `ToLower()` i LINQ-spørringene er enkel å forstå og fungerer fint for dette datavolumet.

## 5. Vurdering og refleksjon

*(Denne seksjonen fylles ut senere.)*

## 6. Relaterte moduler

| Modul | Relasjon |
|-------|----------|
| **Users** | `ApplicationUser.JobTitleId` kobler brukere til stillingstittel |
| **Department** | `DepartmentService.IsValidManagerAsync` bruker `JobTitle.IsLeader` for å validere avdelingsledere |
| **Documents** | `DocumentJobTitle` knytter dokumenter til stillingstitler for målgruppe (mange-til-mange) |
| **RBAC** | Permissions: `job_titles:read/write/delete` |
| **Auth** | Krever autentisering for alle endepunkter |
