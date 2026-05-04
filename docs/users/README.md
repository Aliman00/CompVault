# Brukermodulen — Moduldokumentasjon

Brukermodulen er kjernen i CompVault og håndterer alle ansatte i systemet. Her går vi gjennom hvordan brukeradministrasjonen er bygget, hvilke relasjoner som finnes til andre moduler, og noen av valgene vi tok underveis.

## 1. Problemstilling og behov

Utgangspunktet for modulen var:
> Hvordan kan en bedrift administrere sine ansatte i et system, med mulighet for å koble dem til avdelinger, ledere, og håndtere ulike ansettelsestyper?

Konkrete krav til løsningen:
- Kunne administrere brukere med fornavn, etternavn, stillingstittel (FK til `JobTitle`) og e-post.
- Koble ansatte til avdelinger via `DepartmentId`, slik at organisasjonsstrukturen gjenspeiles i systemet.
- Bygge en lederstruktur der hver ansatt kan ha én nærmeste leder, og en leder kan ha flere direkte underordnede.
- Skille mellom ansettelsestyper via `EmploymentType`-enum: `Permanent` (fast), `Temporary` (midlertidig) og `Contracted` (innleid).
- Bruke soft delete fremfor permanent sletting, slik at historikk og relasjoner bevares.
- Håndtere rolle- og tilgangskobling via ASP.NET Identity, integrert med RBAC-modulen.

## 2. Teknisk design

### Datamodell

Kjernen i brukermodulen er `ApplicationUser`, som arver fra `IdentityUser<Guid>` og utvider med egne felt. Den fullstendige datamodellen er dokumentert i `users-er-diagram.pdf`.

**ApplicationUser:**

| Felt | Type | Hva det er |
|------|------|------------|
| `Id` | Guid | Primærnøkkel (arves fra `IdentityUser<Guid>`) |
| `Email` | varchar(256) | Brukes både som e-post og brukernavn |
| `FirstName` | varchar(100) | Fornavn |
| `LastName` | varchar(100) | Etternavn |
| `JobTitleId` | Guid? | FK til JobTitle — brukerens stillingstittel |
| `EmploymentType` | varchar(20) | `Permanent`, `Temporary` eller `Contracted` |
| `IsActive` | bool | `true` = aktiv konto |
| `DeletedAt` | DateTime? | Soft delete — satt når brukeren slettes |
| `CreatedAt` | DateTime | Når brukeren ble opprettet (UTC) |
| `DepartmentId` | Guid | FK til Department — hvilken avdeling brukeren tilhører |
| `ManagerId` | Guid? | Selvrefererende FK — nærmeste leder |
| `CreatedById` | Guid? | Selvrefererende FK — hvem som opprettet kontoen |

### Lederstruktur

Lederforholdet er modellert som en selvrefererende kobling: `ManagerId` peker tilbake på `ApplicationUser`-tabellen, og navigasjonsegenskapen `DirectReports` gir tilgang til alle underordnede. Det er en én-til-mange-modell — hver bruker har maksimalt én leder, men kan ha flere direkte underordnede.

Modellen er enkel, men dekker behovet. Vi validerer at en bruker ikke kan settes som sin egen leder, og at lederen må være aktiv og tilhøre en avdeling innlogget bruker har tilgang til (via `DepartmentScopeService`).

### Ansettelsestyper

`EmploymentType`-enum skiller mellom `Permanent`, `Temporary` og `Contracted`. Den lagres som en streng i databasen (via `HasConversion<string>()`) og vises direkte i `UserDto`. Det er lite logikk knyttet til dette feltet — det er primært et metadata-felt for presentasjon og filtrering.

### Arkitektur

Modulen er delt i controller, service, repository og mapper. `UserService` samler forretningslogikken og bruker både `UserRepository`, `DepartmentRepository`, `JobTitleRepository`, ASP.NET Identity (`UserManager`, `RoleManager`) og `DepartmentScopeService` for avdelingsbaserte tilgangssjekker. `UserMapper` konverterer mellom entiteter og DTO-er. Samspillet er vist i `users-arkitektur.png`.

**Komponentoversikt:**

| Komponent | Type | Ansvar |
|-----------|------|--------|
| `UsersController` | Controller | 7 endepunkter: paginert liste, enkeltbruker, lookup (permission-styrt), potensielle ledere, opprett, oppdater (partial), slett (soft). |
| `UserService` | Service | Validerer e-post-unikhet, avdeling, leder, stillingstitler og roller. Partial update med ClearFlags. Soft delete. Scope-sjekk på ledertilknytning. |
| `UserMapper` | Statisk klasse | `ApplicationUser → UserDto` (inkl. roller, avdelingsnavn, ledernavn, stillingstittel). `ApplicationUser → UserLookupDto` (for dropdowns). |
| `UserRepository` | Repository | `GetByEmailAsync`, `GetByIdIgnoringFiltersAsync`, `GetByIdWithDetailsAsync`, `GetUsersByTargetAsync`, `GetPotentialManagersAsync`, `SoftDeleteAsync`, `CountActiveAsync`, `GetActiveUsersWithRolesPagedAsync`, `GetLookupAsync`. |
| ASP.NET Identity | Framework | `UserManager` (CRUD for brukere, rollehåndtering), `RoleManager` (rolle-validering). |

### Designvalg

| Valg | Hvorfor |
|------|--------|
| **Arv fra IdentityUser\<Guid\>** | Gir ferdig brukerhåndtering og rollestyring. Vi slipper å bygge autentisering, passord-hashing og lockout selv. |
| **Soft delete** | `DeletedAt` + `IsActive = false`. Globalt EF Core query-filter skjuler slettede brukere automatisk. Historikk og relasjoner bevares. |
| **Selvrefererende leder** | `ManagerId` FK tilbake på egen tabell. Enkelt i databasen, ingen ekstra relasjonstabell. |
| **Partial update med ClearFlags** | `UpdateUserRequest` har kun nullable felt. `ClearJobTitleId` og `ClearManagerId` lar klienten eksplisitt fjerne tilknytninger. `DepartmentId` kan ikke cleares — en bruker må alltid tilhøre en avdeling. |
| **E-post som brukernavn** | `Email.ToLowerInvariant()` settes som både `UserName` og `Email`. Forenkler innlogging — én identifikator. |
| **Paginering** | `GetAllUsersAsync` bruker `PagedQuery` og `GetActiveUsersWithRolesPagedAsync` — henter roller via join i samme spørring, unngår N+1. |
| **Avdelings-scope på ledertilknytning** | `DepartmentScopeService.IsAllowed()` sjekker at innlogget bruker faktisk har tilgang til lederens avdeling før tilknytning tillates. |
| **Ingen dedikerte validators** | Validering skjer i service-laget (forretningsregler) og via DataAnnotation-attributter på DTO-ene. Ingen `FluentValidation` eller lignende. |

## 3. Implementasjon

`UserService` er den sentrale brikken. `GetAllUsersAsync` kjører en paginert spørring via `GetActiveUsersWithRolesPagedAsync` som henter brukere med Department, Manager og JobTitle inkludert, og slår opp roller via en join mot Identity sine tabeller — alt i én databaseoperasjon. Totalantallet hentes separat med `CountActiveAsync` for pagineringsmetadata.

`CreateUserAsync` validerer ganske mye før brukeren faktisk opprettes. E-post sjekkes mot eksisterende brukere, avdeling må eksistere og være aktiv (`DepartmentRepository.ExistsAsync`), og stillingstittel må finnes og være aktiv (`JobTitleRepository.ExistsAsync`). Lederen sjekkes med `GetByIdIgnoringFiltersAsync` fordi lederen kan være inaktiv — men da nektes tilknytningen. I tillegg kjøres en scope-sjekk: innlogget bruker må ha tilgang til lederens avdeling. Roller valideres én og én via `RoleManager.RoleExistsAsync()` før brukeren opprettes, og tildeles deretter med `UserManager.AddToRolesAsync`.

`UpdateUserAsync` er en partial update — bare felter som faktisk er satt i requesten blir endret. `ClearJobTitleId` og `ClearManagerId` lar klienten eksplisitt fjerne tilknytninger. `DepartmentId` kan ikke cleares — en bruker må alltid ha en avdeling. Rolleoppdatering skjer i en transaksjon: fjern alle eksisterende roller, legg til de nye. Etter oppdateringen hentes brukeren på nytt for å sikre at frontend får ferske data.

`DeleteUserAsync` er en soft delete — `SoftDeleteAsync` setter `DeletedAt = DateTime.UtcNow` og `IsActive = false`. Ingen avhengigheter ryddes opp; det globale query-filteret sørger for at brukeren forsvinner fra alle vanlige spørringer.

`UserRepository` har 9 metoder. `GetActiveUsersWithRolesPagedAsync` er den mest komplekse — den bruker `Select` med en subquery for roller, `Join` mot `Roles`-tabellen, og `Skip`/`Take` for paginering. `GetPotentialManagersAsync` filtrerer på `JobTitle.IsLeader == true`. `GetLookupAsync` støtter avdelingsbasert filtrering via `allowedDepartmentIds` og `bypass`-flagg.

`UserMapper` er en statisk klasse med to metoder. `ToDto` mapper alle felter fra `ApplicationUser` til `UserDto`, inkludert `ManagerName` (Fornavn + Etternavn), `DepartmentName`, `JobTitleName` og `Roles`. `ToLookupDto` er en extension method som gir et kompakt DTO for dropdowns: `Id`, `FullName`, `DepartmentName`, `JobTitleName`.

## 4. Utfordringer og beslutninger

### Soft delete vs. hard delete

En av de aller første diskusjonene var om brukere skulle slettes permanent. Problemet er at brukere er koblet til kompetanser, dokumenter, utstyr, revisjonslogger og mye annet. Permanent sletting ville enten krevd cascading deletes — med tap av historikk — eller etterlatt seg masse foreldreløse referanser. Soft delete var den åpenbare løsningen: brukeren markeres som slettet og forsvinner fra alle vanlige spørringer, men dataene består. Det globale EF Core query-filteret (`HasQueryFilter`) gjør dette transparent for all kode som spør mot `ApplicationUser` — vi slipper å huske på `Where(u => u.DeletedAt == null)` overalt.

### Partial update med ClearFlags

Vi ville unngå at klienten må sende hele brukerobjektet for å endre ett felt. Løsningen ble `UpdateUserRequest` der alle felt er nullable, og bare det som faktisk er satt blir oppdatert. Utfordringen var hvordan man fjerner en tilknytning (f.eks. fjerne leder). En `null`-verdi er tvetydig — betyr det "ikke endre" eller "fjern"? Vi løste det med `ClearJobTitleId` og `ClearManagerId` som eksplisitte bool-flagg. `DepartmentId` fikk ikke tilsvarende flagg fordi hver bruker alltid må ha en avdeling — det er et forretningskrav.

### Scope-sjekk på ledertilknytning

Når en admin setter en leder for en bruker, må vi sikre at adminen faktisk har tilgang til lederens avdeling. Uten dette kunne en admin med begrenset scope (f.eks. kun "Småbarns avdeling") sett en bruker under en leder i "Ledelse" — som adminen ikke skal ha tilgang til. `DepartmentScopeService.IsAllowed()` med `UsersAll` og `UsersReadSub` løser dette.

### E-post = brukernavn

Vi valgte å sette `UserName` lik `Email.ToLowerInvariant()`. Dette betyr at `CreateUserRequest.Email` både er kontaktinformasjon og innloggingsidentifikator. Ved e-postbytte i `UpdateUserAsync` oppdateres derfor fire felter: `Email`, `NormalizedEmail`, `UserName` og `NormalizedUserName`. Det er en enkel modell, men den fungerer godt så lenge e-postadresser er unike — noe vi allerede validerer.

## 5. Vurdering og refleksjon

*(Denne seksjonen fylles ut senere.)*

## 6. Relaterte moduler

| Modul | Relasjon |
|-------|----------|
| **Auth** | `ApplicationUser` er identitetsbærer under autentisering; `OtpCodes` og `RefreshTokens` er bundet til brukeren |
| **Department** | `DepartmentId` (FK) + `Department`-navigasjon; `ManagerId` er selvrefererende lederstruktur |
| **JobTitles** | `JobTitleId` (FK) — brukerens stillingstittel; `GetPotentialManagersAsync` filtrerer på `IsLeader` |
| **Competencies** | `Competency.UserId` peker på `ApplicationUser` |
| **Documents** | `DocumentSignature.UserId` peker på `ApplicationUser`; `GetUsersByTargetAsync` brukes av `DocumentSignatureService` |
| **Equipment** | `EquipmentIssuance.UserId` peker på `ApplicationUser` |
| **RBAC** | Roller kobles via `AspNetUserRoles` (Identity); `RoleManager` validerer roller; scope-sjekker via `DepartmentScopeService` |
