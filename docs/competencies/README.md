# Kompetansemodulen — Moduldokumentasjon

Kompetansemodulen holder oversikt over kurs, sertifikater og annen opplæring ansatte har i CompVault. Her var hovedpoenget å få en løsning som både håndterer tildeling av kompetanse og følger med på når noe nærmer seg utløp — uten at noen må sitte og følge med manuelt.

## 1. Problemstilling og behov

Utgangspunktet for modulen var:
> Hvordan kan en bedrift holde oversikt over hvilke ansatte som har gyldige sertifikater, kurs og HMS-opplæring, og bli varslet før noe utløper?

Konkrete krav til løsningen:
- Kunne sette opp ulike typer kompetanse, med kategori og konfigurasjon for om typen krever utløpsdato.
- Koble kompetansebevis til en bestemt ansatt, med utstedelsesdato og eventuell utløpsdato.
- Regne ut status automatisk — om et bevis er gyldig, utløper snart, eller allerede har gått ut.
- Kunne tilbakekalle et bevis (revoke), med krav om begrunnelse.
- Statusene må oppdateres jevnlig av en bakgrunnsjobb.
- Brukerne må kunne filtrere på ansatt, avdeling, status og kompetansetype.
- Sende e-postvarsler til ansatte og deres ledere når bevis nærmer seg utløp.

## 2. Teknisk design

### Datamodell

Modulen har to hovedentiteter: `CompetencyType` (malen) og `Competency` (det konkrete beviset). Den fullstendige datamodellen med indekser og relasjoner er dokumentert i `competencies-er-diagram.pdf`.

**CompetencyType:**
| Felt | Type | Hva det er |
|------|------|------------|
| `Id` | Guid | Primærnøkkel |
| `Name` | varchar(200) | Navn, f.eks. "Førerkort klasse B" |
| `Description` | varchar(500) | Valgfri beskrivelse |
| `Category` | varchar(100) | Gruppering, f.eks. "HMS", "Sertifikat", "Kurs" |
| `RequiresExpiration` | bool | Om typen krever utløpsdato (default: true) |
| `CreatedAt` | DateTime | Når typen ble opprettet |
| `IsActive` | bool | Om typen er aktiv (default: true) |
| `DeletedAt` | DateTime? | Soft delete |

**Competency:**
| Felt | Type | Hva det er |
|------|------|------------|
| `Id` | Guid | Primærnøkkel |
| `UserId` | Guid | FK til ApplicationUser — hvem som har beviset |
| `CompetencyTypeId` | Guid | FK til CompetencyType — hvilken type bevis |
| `Status` | CompetencyStatus | Valid, ExpiringSoon, Expired eller Revoked |
| `IssuedDate` | DateTime | Når beviset ble utstedt |
| `ExpiryDate` | DateTime? | Når det utløper (null hvis typen ikke krever) |
| `CertificateNumber` | varchar(100) | Valgfritt sertifikatnummer |
| `Notes` | varchar(2000) | Valgfrie notater |
| `RevokedAt` | DateTime? | Når beviset ble tilbakekalt |
| `RevokedReason` | string | Årsak til tilbakekalling (påkrevd ved revoke) |
| `CreatedAt` | DateTime | Når beviset ble opprettet |
| `IsActive` | bool | Om beviset er aktivt (default: true) |
| `DeletedAt` | DateTime? | Soft delete |

**Viktige relasjoner:**
- `Competency` → `ApplicationUser`: Many-to-One, `OnDelete: Cascade`
- `Competency` → `CompetencyType`: Many-to-One, `OnDelete: Restrict` (kan ikke slette type med aktive bevis)

**Avdelingsfiltrering:**
- `Competency` har ingen direkte kobling til `Department`. Filtrering skjer via `ApplicationUser.DepartmentId` i repository-laget gjennom `ApplyDepartmentFilter`.
- `departmentScope.HasBypass(Permissions.CompetenciesAll)` = admin-bypass, ser alle
- `departmentScope.GetAllowedDepartmentIds(Permissions.CompetenciesReadSub)` = filtrer til tillatte avdelinger

### Arkitektur

Modulen har to selvstendige spor — ett for kompetansebevis og ett for kompetansetyper — i tillegg til to bakgrunnsjobber. Samspillet er vist i `competencies-arkitektur.png`.

**Komponentoversikt:**

| Komponent | Type | Ansvar |
|-----------|------|--------|
| `CompetenciesController` | Controller | CRUD for kompetansebevis med paginering og filtrering. `competencies:read/write/delete`. |
| `CompetencyTypesController` | Controller | CRUD for kompetansetyper. `competencies:read/write/delete`. |
| `CompetencyService` | Service | Forretningslogikk: validerer type + bruker, sjekker RequiresExpiration, håndterer revoke med RevokedReason, beregner status via Calculator, sjekker avdelingstilgang for target-bruker. |
| `CompetencyTypeService` | Service | CRUD for typer: navneunikhet (case-insensitive), beskytter sletting av typer med aktive bevis, partial update med nullable felt. |
| `CompetencyMapper` | Statisk klasse | `Competency → CompetencyDto` (inkl. TypeName, UserName, DaysUntilExpiry) og `CompetencyType → CompetencyTypeDto`. |
| `CompetencyStatusCalculator` | Statisk klasse | Beregner status: null → Valid, `<= now` → Expired, `<= now+90d` → ExpiringSoon, ellers Valid. Terskel: 90 dager. |
| `CompetencyRepository` | Repository | `GetWithDetailsAsync`, `GetAllWithDetailsPagedAsync`, `CountWithFiltersAsync`, `GetForUpdateAsync`, `UpdateExpiryStatusesAsync`, `SoftDeleteAsync`. Inneholder `ApplyDepartmentFilter`. |
| `CompetencyTypeRepository` | Repository | `GetByNameAsync` (case-insensitive), `HasCompetenciesAsync` (kun aktive, ikke expired/revoked), `SoftDeleteAsync`. |
| `CompetencyStatusJob` | BackgroundService | Kjører umiddelbart ved oppstart, deretter hver 24. time. Bulk-oppdaterer statuser via `ExecuteUpdateAsync`. Berører aldri Revoked. Logger audit manuelt. |
| `ExpiryNotificationJob` | BackgroundService | Kjører hver 24. time. Sender e-postvarsler til ansatte + ledere ved terskler: 90, 60, 30, 14, 7, 0 dager før utløp. Deduplisering via `CompetencyNotificationLog`. |
| `ICompetencyNotificationRepository` | Repository | Brukes av `ExpiryNotificationJob`: `HasBeenSentAsync`, `AddAsync`, `DeleteForCompetencyAsync`. |

### Designvalg

| Valg | Hvorfor |
|------|--------|
| **To separate spor** | Kompetansetyper og kompetansebevis er ulike nok til at det ga mening med to controllere og to services. De deler mapper og databaselag. |
| **Statisk Calculator** | All statuslogikk på ett sted. `ExpiringSoonThresholdDays = 90` er en konstant — lett å trekke ut til config senere. |
| **Kun Revoked kan settes manuelt** | Alle andre statuser beregnes automatisk av Calculator. Hindrer at noen setter "Valid" på et utløpt bevis. |
| **RevokedReason påkrevd** | Tvinger frem en begrunnelse ved tilbakekalling. Viktig for audit-sporing. |
| **Soft delete overalt** | `DeletedAt` + `IsActive` + query-filter. Ingen permanent sletting. |
| **RequiresExpiration kan ikke endres hvis typen har aktive bevis** | Hindrer inkonsistens — du kan ikke plutselig fjerne utløpskravet for en type som allerede har utløpsdatoer på eksisterende bevis. |
| **To bakgrunnsjobber** | `CompetencyStatusJob` oppdaterer statuser. `ExpiryNotificationJob` sender e-post. Separert fordi de har ulike formål og feilhåndtering. |
| **Deduplisering av e-postvarsler** | `CompetencyNotificationLog` sørger for at hver kombinasjon av (kompetanse, terskel, e-post) kun varsles én gang. |
| **Manuelle navigasjoner i ExpiryNotificationJob** | Bakgrunnsjobber har ingen autentisert bruker, så `DepartmentScope`-filteret fjerner alle brukere. Løsningen: hent brukere separat med `IgnoreQueryFilters` og wire opp navigasjon manuelt. |

## 3. Implementasjon

Kjernen i hele kompetansebiten er egentlig ganske rett fram — det er `CompetencyType` som er malen og `Competency` som er det faktiske beviset en ansatt har. `CompetencyType` sier hva slags kompetanse det er, om den krever utløpsdato, og hvilken kategori den tilhører (HMS, sertifikat, kurs og så videre). `Competency` er selve koblingen — her ligger utstedelsesdato, utløpsdato, status, og eventuelt sertifikatnummer og notater.

Når noen oppretter et bevis gjennom `CompetencyService.CreateAsync`, går vi gjennom en del sjekker først. Finnes kompetansetypen? Er den aktiv? Finnes brukeren, og har vi lov til å legge til kompetanse på denne brukeren (avdelingssjekk)? Hvis typen krever utløpsdato — er den faktisk satt? Er utløpsdatoen etter utstedelsesdatoen? Først når alt dette er på plass, lagres beviset. Statusen beregnes automatisk av `CompetencyStatusCalculator` — vi lar aldri noen sette den manuelt, med ett unntak: Revoked.

Oppdatering (`UpdateAsync`) er et av de stedene der det skjedde litt underveis. Vi startet med en `AsNoTracking`-spørring, men det ble problemer fordi ChangeTracker ikke kjente entiteten og vi måtte gjøre ekstra kall. Løsningen ble `GetForUpdateAsync` — en tracking query med Include som henter både `ApplicationUser` og `CompetencyType` samtidig. Ved revoke krever vi `RevokedReason` og setter audit-kontekst så interceptoren vet hva som skjedde. Hvis `ExpiryDate` endres (typisk ved fornyelse), kalkulerer vi ny status — med mindre beviset allerede er revokert — og sletter gammel varslingslogg så varslingssyklusen starter på nytt.

`CompetencyTypeService` er enklere. Navn må være unike (case-insensitive, vi bruker `ToLower()` i spørringen). Hvis noen prøver å endre `RequiresExpiration` på en type som allerede har aktive bevis, stopper vi det. Samme med sletting — en type med bevis på seg får bli.

Det som skiller denne modulen fra mange andre er de to bakgrunnsjobbene. `CompetencyStatusJob` kjører umiddelbart ved oppstart og deretter hver 24. time. Den finner alle bevis som har passert utløpsdato eller er innenfor 90-dagersgrensen, og bulk-oppdaterer statusene med `ExecuteUpdateAsync`. Siden dette går utenom ChangeTracker, skriver den `AuditLog`-entries manuelt for hver endring.

`ExpiryNotificationJob` er den andre. Den sjekker 6 terskler — 90, 60, 30, 14, 7 og 0 dager før utløp — og sender e-post til både den ansatte og lederen. Her fikk vi en interessant utfordring: bakgrunnsjobber har ingen autentisert bruker, så `DepartmentScope`-filteret på `ApplicationUser` sier "ingen avdelinger tillatt" og fjerner alle brukere. Løsningen er å hente kompetanser uten `Include` på `ApplicationUser`, og så hente brukere og ledere i separate spørringer med `IgnoreQueryFilters()`. `CompetencyNotificationLog` sørger for at ingen får samme varsel to ganger.

## 4. Utfordringer og beslutninger

### Oppdatering var vanskeligere enn forventet

`GetWithDetailsAsync` brukte `AsNoTracking`, så når vi prøvde å oppdatere et bevis var entiteten detached og måtte trackes på nytt. Det betydde `UpdateAsync` + en ekstra query for å hente navigasjon etterpå, og det ble rotete.

Løsningen ble en egen metode, `GetForUpdateAsync`, som er en tracking query med Include. Da ligger entiteten i ChangeTracker fra start, og vi slipper både `UpdateAsync` og den ekstra spørringen på slutten. Det høres kanskje ut som en liten ting, men i praksis gjorde det oppdateringskoden mye renere.

### Avdelingsfiltrering måtte være på SQL-nivå

Kompetansebevis har ingen direkte kobling til avdeling, bare til bruker. Så for å filtrere på avdeling måtte vi gå via `ApplicationUser.DepartmentId`. Hvis vi hadde gjort dette i minnet, måtte vi laste alle bevis og filtrere etterpå — det skalerer dårlig.

I stedet la vi filtreringen inn i SQL-spørringen via `ApplyDepartmentFilter`. Den slår opp tillatte avdelinger gjennom `DepartmentScopeService` og bruker `allowedUserIds.Contains(c.UserId)` direkte i `WHERE`-klausulen. Admin-brukere med `CompetenciesAll` hopper over hele filteret.

### Bakgrunnsjobben hadde ingen bruker

Da vi skulle lage `ExpiryNotificationJob`, kræsjet det umiddelbart. Bakgrunnsjobber har ingen `HttpContext`, så det er ingen autentisert bruker. `DepartmentScope`-filteret på `ApplicationUser` svarte med "ingen avdelinger tillatt", og vipps — alle brukere var borte. Null kompetansebevis å varsle om.

Vi endte med en todelt fiks: ikke `Include` `ApplicationUser` i den første spørringen (da slapp vi at filteret fjernet alt), og hent deretter brukere og ledere i separate spørringer med `IgnoreQueryFilters()`. Så wiret vi opp navigasjonen manuelt. Det er ikke superpent, men det fungerer, og alternativet hadde vært å bygge om hele scope-systemet for bakgrunnsjobber.

### Bulk og audit er ikke venner

`ExecuteUpdateAsync` er kjapp, men går helt utenom EF Cores ChangeTracker. Det betyr at `AuditSaveChangesInterceptor` aldri ser endringene — og vi får ingen revisjonslogg. Det er dumt, spesielt for noe så viktig som statusendringer på kompetansebevis.

Løsningen ble at `CompetencyStatusJob` skriver `AuditLog`-entries manuelt. Vi henter ut alle berørte bevis med gammel og ny status FØR bulk-oppdateringen, og skriver én audit-rad per endring. Det er litt ekstra jobb, men vi slipper å ofre revisjonssporet.

### Droppet AutoMapper

Vi diskuterte AutoMapper, men med bare to entiteter og to DTO-er føltes det som å skyte spurv med kanon. `CompetencyMapper` er en statisk klasse på under 60 linjer, og det er helt åpenbart hva som skjer når du leser den. Ingen magi, ingen konfigurasjon, ingenting som brekker hvis noen endrer et feltnavn uten å oppdatere en AutoMapper-profil.

## 5. Relaterte moduler

| Modul | Relasjon |
|-------|----------|
| **Users** | `Competency.UserId` peker på `ApplicationUser`; avdelingsfiltrering via `ApplicationUser.DepartmentId` |
| **Department** | Avdelings-scoping via `DepartmentScopeService`; `CompetencyStatusJob` og `ExpiryNotificationJob` bruker `DepartmentScope` |
| **RBAC** | Permissions: `competencies:read/write/delete`, `competencies:read:sub`, `competencies:all` |
| **Auth** | Krever autentisering for alle endepunkter; `ExpiryNotificationJob` sender e-post via `EmailService` |
