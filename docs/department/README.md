# Avdelingsmodulen — Moduldokumentasjon

Avdelingsmodulen håndterer hvordan bedriften er organisert i CompVault. Mye av jobben her handlet om å få avdelinger og underavdelinger til å henge sammen, uten at vi åpnet for tullekoblinger i hierarkiet.

## 1. Problemstilling og behov

Utgangspunktet for modulen var:
> Hvordan kan en bedrift modellere sin organisasjonsstruktur med hierarkiske avdelinger, og koble ansatte til riktig avdeling?

Konkrete krav til løsningen:
- Kunne opprette avdelinger med navn og beskrivelse.
- En avdeling skal kunne ligge under en annen avdeling (hierarki).
- Ansatte må kunne kobles til avdeling via `ApplicationUser.DepartmentId`.
- En avdeling må kunne ha en leder — en bruker med en stillingstittel som er markert som lederstilling.
- Modellen må hindre sirkulære referanser (en avdeling kan ikke være sin egen besteforelder).
- Det skal ikke være mulig å slette en avdeling som fortsatt har medlemmer eller underavdelinger.
- Soft delete — historikken skal bestå.

## 2. Teknisk design

### Datamodell

Kjernen er `Department`-entiteten med en selvrefererende `ParentDepartmentId`. Den fullstendige datamodellen er dokumentert i `department-er-diagram.pdf`.

**Department:**
| Felt | Type | Hva det er |
|------|------|------------|
| `Id` | Guid | Primærnøkkel |
| `Name` | varchar(200) | Avdelingsnavn |
| `Description` | varchar(500) | Valgfri beskrivelse |
| `ParentDepartmentId` | Guid? | Overordnet avdeling (null = toppnivå) |
| `ManagerId` | Guid? | Leder for avdelingen (FK til ApplicationUser) |
| `CreatedById` | Guid? | Hvem som opprettet avdelingen |
| `CreatedAt` | DateTime | Når avdelingen ble opprettet |
| `IsActive` | bool | Om avdelingen er aktiv (default: true) |
| `DeletedAt` | DateTime? | Soft delete |

**Hierarkiet:**
- `ParentDepartmentId` peker tilbake på samme tabell (`OnDelete: SetNull`).
- Underavdelinger finnes via `SubDepartments`-navigasjonen.
- API-et returnerer en flat liste med `ParentDepartmentId` og `SubDepartmentCount`, så frontend bygger treet selv.

**Leder:**
- `ManagerId` peker på en `ApplicationUser`. Ved opprettelse og oppdatering valideres lederen via `IsValidManagerAsync`: brukeren må eksistere, være aktiv, og ha en `JobTitle` der `IsLeader = true`.
- `ClearManagerId` i `UpdateDepartmentRequest` fjerner lederen.

### Arkitektur

`DepartmentService` håndterer forretningslogikken, `DepartmentRepository` tar seg av databasekallene, og `DepartmentMapper` konverterer til DTO. Samspillet er vist i `department-arkitektur.png`.

**Komponentoversikt:**

| Komponent | Type | Ansvar |
|-----------|------|--------|
| `DepartmentsController` | Controller | CRUD. `departments:read/write/delete`. |
| `DepartmentService` | Service | Validerer hierarki (sirkulær-sjekk), validerer leder (IsLeader), beskytter sletting (underavdelinger/medlemmer), partial update. |
| `DepartmentMapper` | Statisk klasse | `Department → DepartmentDto`: Name, Description, ManagerName, CreatedByName, SubDepartmentCount. |
| `DepartmentRepository` | Repository | `GetByIdWithHierarchyAsync`, `GetAllWithHierarchyAsync`, `HasSubDepartmentsAsync`, `HasMembersAsync`, `GetAncestorIdsAsync`, `SoftDeleteAsync`. |

### Designvalg

| Valg | Hvorfor |
|------|--------|
| **Selvrefererende hierarki** | `ParentDepartmentId` i samme tabell. Enkelt, fleksibelt, ingen egen relasjonstabell. |
| **Flat API-respons** | Returnerer liste med `ParentDepartmentId` og `SubDepartmentCount`. Frontend bygger treet — backend slipper rekursiv JSON. |
| **SetNull ved sletting av forelder** | Underavdelinger overlever og blir toppnivå. Tryggere enn cascade. |
| **Sirkulær validering** | `GetAncestorIdsAsync` henter alle avdelinger (kun Id + ParentDepartmentId), traverserer oppover i minnet. Hvis ny forelder finnes blant ancestors: stopp. |
| **Leder-validering** | `IsValidManagerAsync` sjekker at brukeren finnes, er aktiv, og har `JobTitle.IsLeader = true`. Ikke alle kan settes som avdelingsleder. |
| **Beskyttet sletting** | Sjekker `HasSubDepartmentsAsync` og `HasMembersAsync` før sletting. Gir tydelig feilmelding i stedet for å ødelegge data. |
| **GetAllWithHierarchyAsync med IgnoreQueryFilters** | Bruker `.IgnoreQueryFilters()` + eksplisitt `WHERE IsActive AND DeletedAt IS NULL`. Nødvendig fordi query-filteret ellers ville fjernet inaktive foreldre som har aktive underavdelinger. |

## 3. Implementasjon

`DepartmentService` er hjertet i modulen. `CreateAsync` sjekker at overordnet avdeling finnes (hvis satt), validerer lederen via `IsValidManagerAsync`, og lagrer. `UpdateAsync` støtter partial update — `null` betyr "ikke endre", og `ClearParentDepartment`/`ClearManagerId` brukes for å fjerne tilknytninger.

Sirkulær-referanse-sjekken i `UpdateAsync` er verdt å nevne. Når noen setter en ny `ParentDepartmentId`, henter vi ALLE avdelinger fra databasen og traverserer oppover fra den nye forelderen via `GetAncestorIdsAsync`. Hvis den opprinnelige avdelingen dukker opp i ancestor-lista, betyr det at vi prøver å lage en loop — da stoppes det med en valideringsfeil. Dette er ikke den mest elegante løsningen for en organisasjon med 100 000 avdelinger, men for en barnehage funker det helt fint.

`GetAllWithHierarchyAsync` fortjener også en kommentar. Den bruker `IgnoreQueryFilters()` fordi vi trenger å se hele hierarkiet — inkludert inaktive avdelinger som har aktive underavdelinger. Uten dette ville query-filteret (`DeletedAt == null`) fjernet en inaktiv forelder, og underavdelingene ville sett ut som de hang i løse lufta. I stedet filtrerer vi manuelt med `WHERE IsActive AND DeletedAt IS NULL`.

`DepartmentMapper` er enkel — den tar en `Department` og et tall for `SubDepartmentCount`, og mapper til DTO. Den inkluderer også `ManagerName` (fornavn + etternavn) og `CreatedByName`, hentet fra navigasjonsegenskapene.

## 4. Utfordringer og beslutninger

### Sirkulære referanser

Når avdelinger ligger i et tre, kan ikke strukturen peke tilbake på seg selv. Hvis "IT" ligger under "Ledelsen", kan ikke "Ledelsen" flyttes under "IT". Vi vurderte rekursiv SQL, men endte med å laste alle avdelinger (bare Id og ParentDepartmentId) og traversere i minnet. For vår datamengde er det helt uproblematisk, og koden er mye lettere å lese.

### SetNull eller Cascade?

Hvis en overordnet avdeling slettes — hva skjer med underavdelingene? Cascade ville slettet hele grenen, noe som er katastrofalt i en bedrift. Vi valgte `SetNull`: underavdelingene overlever, får `ParentDepartmentId = null`, og dukker opp som toppnivå-avdelinger til noen plasserer dem på nytt.

### Flat liste i stedet for nøstet tre

Vi diskuterte om API-et skulle returnere et ferdig bygd JSON-tre. Det høres fint ut, men gjør paginering, filtrering og oppdateringer mer kompliserte. En flat liste med `ParentDepartmentId` er enklere å hente, enklere å cache, og frontend har all infoen den trenger for å bygge treet selv.

### Leder må ha leder-stilling

Vi kunne latt hvem som helst bli satt som avdelingsleder, men det ga ikke mening. Hvis du skal være leder for en avdeling, bør stillingstittelen din faktisk si at du er leder. `IsValidManagerAsync` sjekker `JobTitle.IsLeader` — en enkel validering som hindrer at en sommervikar plutselig blir satt som avdelingsleder ved et uhell.

## 5. Vurdering og refleksjon

*(Denne seksjonen fylles ut senere.)*

## 6. Relaterte moduler

| Modul | Relasjon |
|-------|----------|
| **Users** | `ApplicationUser.DepartmentId` kobler brukere til avdeling; `Department.CreatedById` sporer oppretter; `Department.ManagerId` peker på leder |
| **JobTitles** | `JobTitle.IsLeader` brukes i `IsValidManagerAsync` for å validere avdelingsledere |
| **Documents** | `DocumentDepartment` knytter dokumenter til avdelinger for målgruppe |
| **Competencies** | Avdelingsfiltrering av kompetansebevis skjer via `ApplicationUser.DepartmentId` |
| **RBAC** | Permissions: `departments:read/write/delete`, `departments:read:sub`, `departments:all` |
| **Auth** | Krever autentisering for alle endepunkter |
