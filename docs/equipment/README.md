# Utstyrsmodulen — Moduldokumentasjon

Utstyrsmodulen i CompVault holder styr på utstyrskategorier, utstyr, og hvem som har fått utlevert hva. Fra uniformer og verneutstyr til laptops og læremidler — her logges alt som deles ut til ansatte.

## 1. Problemstilling og behov

Utgangspunktet for modulen var:
> Hvordan kan bedriften holde oversikt over alt utstyr som deles ut til ansatte — hva de har fått, når de fikk det, i hvilken størrelse, og hvem som delte det ut?

Konkrete krav til løsningen:
- Kunne definere utstyrskategorier (f.eks. IT-utstyr, uniform, verneutstyr) for å organisere utstyret.
- Kunne opprette spesifikt utstyr under kategorier, med flagg for om utstyret krever størrelse (klær har størrelse, laptop har ikke).
- Registrere utleveringer med antall, størrelse, dato og hvem som delte ut.
- Ansatte skal kunne se sitt eget utstyr, filtrert på kategori.
- Administratorer og ledere skal kunne se utleveringer på tvers av avdelinger, med avdelings-scoping.
- Soft delete på alle nivåer, med beskyttelse mot sletting av kategorier og utstyr som fortsatt er i bruk.

## 2. Teknisk design

### Datamodell

Modulen har tre entiteter i et hierarki: `EquipmentCategory` → `EquipmentItem` → `EquipmentIssuance`. Den fullstendige datamodellen er dokumentert i `equipment-er-diagram.pdf`.

**EquipmentCategory:**
| Felt | Type | Hva det er |
|------|------|------------|
| `Id` | Guid | Primærnøkkel |
| `Name` | varchar(100) | Kategorinavn, unikt (soft-delete aware — indeksfilter: `DeletedAt IS NULL`) |
| `Description` | varchar(300) | Valgfri beskrivelse av kategorien |
| `IsActive` | bool | Om kategorien er aktiv (default: true) |
| `CreatedAt` | DateTime | Når kategorien ble opprettet |
| `DeletedAt` | DateTime? | Soft delete |

**EquipmentItem:**
| Felt | Type | Hva det er |
|------|------|------------|
| `Id` | Guid | Primærnøkkel |
| `CategoryId` | Guid | FK til EquipmentCategory (OnDelete: Restrict) |
| `Name` | varchar(200) | Navn på utstyret, unikt per kategori |
| `HasSize` | bool | Om utstyret har størrelse (true for klær/sko, false for laptop/hjelm) |
| `IsActive` | bool | Om utstyret er aktivt (default: true) |
| `CreatedAt` | DateTime | Når utstyret ble opprettet |
| `DeletedAt` | DateTime? | Soft delete |

**EquipmentIssuance:**
| Felt | Type | Hva det er |
|------|------|------------|
| `Id` | Guid | Primærnøkkel |
| `UserId` | Guid | FK til ApplicationUser — hvem som mottok utstyret (OnDelete: Restrict) |
| `ItemId` | Guid | FK til EquipmentItem — hvilket utstyr som ble utlevert |
| `IssuedById` | Guid | FK til ApplicationUser — hvem som delte ut utstyret (OnDelete: Restrict) |
| `Quantity` | int | Antall utlevert (default: 1) |
| `Size` | varchar(20) | Størrelse. Påkrevd hvis `Item.HasSize = true`. Null ellers. |
| `IssuedDate` | DateTime | Når utstyret ble utlevert |
| `Notes` | varchar(500) | Valgfrie notater |
| `IsActive` | bool | Om utleveringen er aktiv (default: true) |
| `CreatedAt` | DateTime | Når utleveringen ble registrert |
| `DeletedAt` | DateTime? | Soft delete |

**Relasjoner:**
- `EquipmentItem` → `EquipmentCategory`: Many-to-One, `OnDelete: Restrict` — kan ikke slette kategori med aktivt utstyr.
- `EquipmentIssuance` → `ApplicationUser` (UserId): Many-to-One, `OnDelete: Restrict` — mottaker beskyttes.
- `EquipmentIssuance` → `ApplicationUser` (IssuedById): Many-to-One, `OnDelete: Restrict` — utsteder beskyttes.
- `EquipmentIssuance` → `EquipmentItem`: Many-to-One, `OnDelete: Cascade` — slettes utstyr, slettes utleveringene.

**Avdelingsfiltrering:**
- `EquipmentIssuance` har ingen direkte kobling til `Department`. Filtrering skjer via `ApplicationUser.DepartmentId` i `ApplyDepartmentFilter` i repository-laget.
- `equipment:read:all` → admin-bypass, ser alle utleveringer uavhengig av avdeling.
- `equipment:read:sub` → filtrert til tillatte avdelinger via `DepartmentScopeService`.
- Alle spørringer bruker `IgnoreQueryFilters()` + manuell `WHERE DeletedAt IS NULL` — nødvendig fordi soft-delete-filteret på `ApplicationUser` ellers ville fjernet inaktive brukere som har aktive utleveringer.

### Arkitektur

Modulen har tre separate spor — ett for kategorier, ett for utstyr, og ett for utleveringer — med delt mapper og autorisasjonssystem. Samspillet er vist i `equipment-arkitektur.png`.

**Komponentoversikt:**

| Komponent | Type | Ansvar |
|-----------|------|--------|
| `EquipmentCategoriesController` | Controller | CRUD for kategorier. `equipment:read/write/delete`. Sletting nektes hvis kategorien har aktivt utstyr. |
| `EquipmentItemsController` | Controller | CRUD for utstyr, filtrering per kategori. `equipment:read/write/delete`. Sletting nektes hvis utstyret har aktive utleveringer. |
| `EquipmentIssuancesController` | Controller | CRUD for utleveringer + `/my` og `/my/categories` for innlogget bruker. `equipment:read/write/delete`. Create sjekker avdelingstilgang på mottaker. Update låser User, Item og Issuer — kun Quantity, Size og Notes kan endres. |
| `EquipmentCategoryService` | Service | Navn-unikhet, partial update (null = ikke endre), beskytter sletting av kategorier med aktivt utstyr. |
| `EquipmentItemService` | Service | Navn-unikhet per kategori, validerer at kategorien eksisterer og er aktiv, `HasActiveIssuancesAsync`-sjekk før sletting, validerer `CategoryId` og `HasSize`. |
| `EquipmentIssuanceService` | Service | Avansert validering: gyldige GUIDs, `IssuedDate` (ikke fremtid > 1 dag, ikke > 1 år tilbake), avdelingssjekk for mottaker via `DepartmentScope`, `SizeRequired`-sjekk mot `Item.HasSize`. Update låser kjernen — kun `Quantity`, `Size`, `Notes`. |
| `EquipmentMapper` | Statisk klasse | `EquipmentCategory → EquipmentCategoryDto` (inkl. `ItemCount`), `EquipmentItem → EquipmentItemDto` (inkl. `CategoryName`), `EquipmentIssuance → EquipmentIssuanceDto` (inkl. alle navigasjonsdata: `UserName`, `ItemName`, `CategoryName`, `IssuedByName`, `HasSize`). |
| `EquipmentCategoryRepository` | Repository | `GetAllWithItemsAsync`, `GetByIdWithItemsAsync`, `GetByIdWithItemsForUpdateAsync`, `SoftDeleteAsync`. |
| `EquipmentItemRepository` | Repository | `GetAllWithCategoryAsync`, `GetByIdWithCategoryAsync`, `GetByIdTrackedAsync`, `GetByCategoryIdAsync`, `HasActiveIssuancesAsync`, `SoftDeleteAsync`. |
| `EquipmentIssuanceRepository` | Repository | `QueryWithDetails()` (IQueryable for paginering), `GetByIdWithDetailsAsync`, `GetForUpdateAsync`, `GetByUserIdPagedAsync` (med kategorifilter), `GetByItemIdAsync`, `GetCategoriesForUserAsync` (GroupBy + Distinct count), `ApplyDepartmentFilter`, `SoftDeleteAsync`. |

### Designvalg

| Valg | Hvorfor |
|------|--------|
| **Tre-lags hierarki** | `Category → Item → Issuance`. Naturlig organisering — en laptop hører hjemme i IT-utstyr, en t-skjorte i uniform. Ingen flate lister. |
| **HasSize-flagget** | En laptop har ikke størrelse, en jakke har. Ved utlevering valideres `SizeRequired`: hvis `HasSize = true`, må `Size` fylles ut. Hvis `HasSize = false` og noen sender `Size`, ignoreres den stille. |
| **Update låser kjernen** | `UserId`, `ItemId`, `IssuedById` og `IssuedDate` er immutable etter opprettelse. Hvis du utleverte feil utstyr til feil person — slett og opprett på nytt. Dette er bevisst — revisjonssporet blir renere, og vi unngår edge-caser ved endring av mottaker midt i en utleverings livssyklus. |
| **Soft delete med Restrict** | `EquipmentItem.CategoryId: OnDelete Restrict` — du må først slette eller flytte alt utstyret før kategorien kan slettes. `EquipmentIssuance.UserId` og `IssuedById: OnDelete Restrict` — ingen kan slette en bruker som har utleveringer. `EquipmentIssuance.ItemId: OnDelete Cascade` — slettes utstyret, slettes utleveringene (naturlig, de er verdiløse uten utstyret). |
| **IgnoreQueryFilters i alle spørringer** | Query-filteret på `ApplicationUser` (`DeletedAt == null`) fjerner inaktive brukere. Hvis en inaktiv bruker har aktive utleveringer, må disse fortsatt være synlige. `IgnoreQueryFilters()` + manuell `WHERE DeletedAt IS NULL` på `EquipmentIssuance` sikrer at vi ser utleveringer uavhengig av brukerens status, men fortsatt filtrerer vekk slettede utleveringer. |
| **Avdelingsfiltrering via ApplicationUser** | Utleveringer har ingen egen DepartmentId — de går via `User.DepartmentId`. `ApplyDepartmentFilter` oversetter avdelingstillatelser til en liste med `allowedUserIds` og filtrerer med `Contains`. Admin-brukere med `EquipmentAll` hopper over hele filteret. |
| **GetCategoriesForUserAsync med Distinct count** | Grupperer utleveringer per kategori og teller distinkte items (ikke utleveringer). Hvis en ansatt har 4 t-skjorter og 2 bukser, vises "Uniform: 2 utstyr" — ikke "Uniform: 6 utleveringer". `GroupBy` + `Select(Distinct Count)` gir riktig semantikk. |

## 3. Implementasjon

Modulen er delt i tre selvstendige spor, men de følger samme mønster. `EquipmentCategoryService` er den enkleste — standard CRUD med navn-unikhet og beskyttet sletting. `CreateAsync` trimmer navn og sjekker at det ikke finnes fra før. `UpdateAsync` støtter partial update — kun feltene som sendes inn endres (`null` = "ikke endre"). `DeleteAsync` er streng: hvis kategorien har aktive items, returneres en valideringsfeil med instruks om å deaktivere eller slette utstyret først.

`EquipmentItemService` følger samme mønster, men med en ekstra dimensjon: navn-unikhet er scoped til kategori. To kategorier kan ha et item med samme navn (f.eks. "Jakke" i både uniform og verneutstyr), men innenfor én kategori må navnet være unikt. `CreateAsync` validerer også at kategorien finnes, er aktiv, og at `CategoryId` ikke er `Guid.Empty`. `HasActiveIssuancesAsync` sjekker `EquipmentIssuances`-tabellen for aktive utleveringer før sletting tillates.

`EquipmentIssuanceService` er den mest komplekse av de tre. `CreateAsync` er en orkestrering av flere valideringer: Er `UserId`, `ItemId` og `IssuedById` gyldige GUIDs? Finnes mottakeren i databasen? Har vi avdelingstilgang til denne mottakeren (via `DepartmentScope`)? Finnes utstyret og er det aktivt? Hvis `HasSize = true` — er `Size` faktisk fylt ut? Er `IssuedDate` innenfor akseptable grenser (ikke mer enn 1 dag i fremtiden, ikke mer enn 1 år tilbake)?

`UpdateAsync` er bevisst restriktiv. Du kan endre `Quantity` (kanskje du fikk 2 ekstra t-skjorter), `Size` (kanskje feil størrelse ble registrert), og `Notes`. Men `UserId`, `ItemId`, `IssuedById` og `IssuedDate` er låst. Dette er et designvalg — hvis utstyret ble utlevert til feil person, skal utleveringen slettes og gjenskapes. Det er mer tungvint, men revisjonssporet blir krystallklart: "slettet utlevering til X" + "opprettet utlevering til Y" er mye lettere å forstå enn "endret mottaker fra X til Y".

`EquipmentIssuanceRepository` har noen interessante spørringer. `GetCategoriesForUserAsync` bruker `GroupBy` på `Item.CategoryId` og teller deretter distinkte `ItemId`-er per gruppe med `g.Select(i => i.ItemId).Distinct().Count()`. Dette gir "hvor mange ulike typer utstyr har denne personen i hver kategori" — nyttig for kategorivisningen i frontend. `ApplyDepartmentFilter` er en privat metode som brukes i nesten alle spørringer — den oversetter `DepartmentScope` til en liste med `allowedUserIds` og filtrerer utleveringer deretter.

En ting å merke seg: alle spørringer i `EquipmentIssuanceRepository` bruker `IgnoreQueryFilters()`. Dette er fordi query-filteret på `ApplicationUser` (`DeletedAt == null`) ellers ville fjernet utleveringer tilhørende inaktive brukere. I stedet for query-filteret bruker vi manuell `WHERE DeletedAt IS NULL` på `EquipmentIssuance` selv. Resultatet er at vi alltid ser utleveringer, selv om mottakeren er deaktivert, men slettede utleveringer filtreres fortsatt vekk.

## 4. Utfordringer og beslutninger

### Update-låsing: strengt men trygt

Da vi designet `UpdateEquipmentIssuanceRequest`, måtte vi ta stilling til hva som faktisk kan endres på en utlevering etter at den er opprettet. Vi kunne latt alt være redigerbart — bytte utstyret fra en laptop til en mobiltelefon, endre mottakeren fra Anne til Bente, endre datoen. Men det hadde skapt gråsoner i revisjonsloggen: står det "Laptop utlevert til Anne 15. januar", men noen har i ettertid endret mottaker til Bente — hva var den faktiske historien?

Vi valgte å låse `UserId`, `ItemId`, `IssuedById` og `IssuedDate`. Det betyr at hvis du utleverte feil størrelse, endrer du bare `Size`. Hvis du utleverte feil utstyr til feil person, må du slette utleveringen og opprette en ny. Det er mer tungvint i øyeblikket, men revisjonssporet blir entydig og etterprøvbart.

### IgnoreQueryFilters — en nødvendig workaround

Dette er en gjenganger i CompVault. Query-filteret `DeletedAt == null` på `ApplicationUser` er nyttig for 95 % av spørringene — du slipper å tenke på å filtrere vekk slettede brukere. Men når en utlevering har en `UserId` som peker på en inaktiv bruker, og du gjør en `Include(i => i.User)`, så fjerner query-filteret den inaktive brukeren — og dermed også hele utleveringen (på grunn av inner join-oppførsel).

Løsningen er `IgnoreQueryFilters()` på alle spørringer i `EquipmentIssuanceRepository`, kombinert med manuell `WHERE DeletedAt IS NULL` på `EquipmentIssuance`. Utleveringen overlever, selv om mottakeren er inaktiv. Dette er konsekvent med hvordan andre moduler (Competencies, Documents) håndterer samme problem.

### Størrelseslogikk: påkrevd vs. ignorert

`HasSize`-flagget på `EquipmentItem` er enkelt, men får konsekvenser i `CreateAsync` og `UpdateAsync`. Ved opprettelse: hvis `HasSize = true` og `Size` er tomt → valideringsfeil. Hvis `HasSize = false` og noen sender `Size` → vi setter den til `null` stille (ikke en feil, men unødvendig data). Ved oppdatering er det litt mer nyansert — vi sjekker `Item.HasSize` via navigasjonen og validerer deretter.

Dette er en av de små detaljene som er lette å overse, men som gjør API-et mer robust. Du kan sende inn hva som helst, men modulen vil alltid sørge for at dataene er konsistente.

## 5. Relaterte moduler

| Modul | Relasjon |
|-------|----------|
| **Users** | `EquipmentIssuance.UserId` og `IssuedById` peker på `ApplicationUser`; avdelingsfiltrering går via `ApplicationUser.DepartmentId` |
| **Department** | Avdelings-scoping via `DepartmentScopeService` i `EquipmentIssuanceRepository.ApplyDepartmentFilter` og `EquipmentIssuanceService.CreateAsync` |
| **RBAC** | Permissions: `equipment:read/write/delete`, `equipment:read:all`, `equipment:read:sub` |
| **Auth** | Krever autentisering for alle endepunkter; `/my` og `/my/categories` henter `UserId` fra token |
| **Audit** | Alle utstyrsoperasjoner logges automatisk via `AuditSaveChangesInterceptor` |
| **Seed-data** | `BarnehageData.cs` inneholder 5 kategorier, 15 items og ~40 utleveringer for demo |
