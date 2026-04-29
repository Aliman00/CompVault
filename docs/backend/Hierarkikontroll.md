 Avdelingsbasert tilgangskontroll i CompVault

---

## Hva er problemet vi løste?

Før denne implementasjonen hadde ikke CompVault noen mekanisme for å begrense hva en bruker faktisk kan se basert på hvilken avdeling de tilhører. En ansatt i IT-avdelingen kunne hente ut brukere, kompetanser og dokumenter som tilhørte HR eller Ledelse.

Vi trengte en løsning som:
- Automatisk filtrerer data basert på brukerens avdeling
- Ikke krever at hver enkelt controller eller service husker å sjekke tilgang. Det hadde blitt en stor jobb å oppdatere alle endepunkter
- Støtter hierarkisk struktur (en leder kan se ned i hierarkiet, men ikke opp)
- Gi brukere tilattelser som overstyrer denne hierarki sjekken

## DepartmentScopeService

`DepartmentScopeService` kjører en gang per HTTP-request og er registrert som scoped så den lever igjennom en hel forespørsel, men ikke gjennom flere forespørsler.

Når den kalles første gang leser den `department_id` fra JWT-claimet og sjekker hvilke permissions brukeren har. Hvis brukeren har bypass-permission (f.eks. `users:read:all`) returneres umiddelbart uten videre oppslag. Hvis brukeren har sub-permission kjøres et BFS-søk (bredde-først) nedover i avdelingshierarkiet fra brukerens avdeling. Dette henter alle avdelinger en gang fra `DepartmentRepository` og traverserer dem i minnet. Avdelingene caches i en `Lazy<T>` for resten av requesten, så BFS og databasekall kjøres aldri mer enn en gang uansett hvor mange databasespørringer som går gjennom forespørselen.

De tre metodene som brukes utenfra:

- `HasBypass(permission)` — returnerer true hvis brukeren har full overstyring
- `GetAllowedDepartmentIds(subPermission)` — returnerer listen over tillatte avdelings-IDer
- `IsAllowed(targetDepartmentId, allPermission, subPermission)` — brukes i service-laget for manuell sjekk ved skriveoperasjoner

---

Brukeren selv trenger ikke å gjøre noe — filtreringen skjer usynlig på databasenivå.

## Tilgangslag — hvordan de henger sammen

Det er 3 forskjellige lag som styrer hva en bruker har tilattelse til:

### Lag 1 — Permissions på kontrollerne

Hver kontroller har en Authorize-attribute med en policy som styrer hva brukeren har lov til å gjøre. Dette har ingenting med hierarkiet å gjøre. ASP.NET Core sin autorisasjonsmekanisme stopper requesten i det hele tatt før det når kontrolleren hvis brukeren ikke har riktig permission. Permission eksempeler:

- `users:read` — du har lov til å se brukere
- `users:write` — du har lov til å opprette og endre brukere
- `users:delete` — du har lov til å slette brukere
- `competencies:read` — du har lov til å se ansattkompetanser
- `equipment:write` — du har lov til å utlevere utstyr

### Lag 2 — Avdelingsscope for brukere (query filter + interceptor)

For `ApplicationUser` er en service kalt `DepartmentScopeService` implementert globalt i `AppDbContext`. Det betyr at alle spørringer mot brukertabellen automatisk filtreres basert på den innloggede brukerens avdeling. Dermed trenger vi ikke å kontrollere dette i servicene eller repositoriene. Hvis f.eks. en tabell har en JOIN mot en User-tabell, så slår filteringen inn. 

For leseoperasjoner håndteres dette av et global query filter som kjører på alle spørringer mot `ApplicationUser`-tabellen. For skriveoperasjoner håndteres det av `DepartmentScopeSaveChangesInterceptor` som sjekker at du ikke kan opprette eller endre en bruker i en avdeling du ikke har tilgang til.

Dette håndterer brukere, og fungerer som en siste sikkerhetssjekk for andre entiteter som f.eks. Equipment og Competencies, hvis vi ikke håndtere det med feature-spesifikke filter som vi gjør i lag 3. For å overstyre bruker filteringen så må man enten ha users:read:sub for å se brukere i underavdelinger, eller users:read:all for å se brukere i alle avdelinger.

### Lag 3 — Feature-spesifikke filter

For andre entiteter enn brukere er det mer komplisert med et filter, og vi har valgt filtrering i repository og service-lagene.

**Equipment** — `EquipmentIssuanceRepository` har en `ApplyDepartmentFilter`-metode som sjekker om utleveringen tilhører en bruker i en avdeling du har tilgang til. Den bruker `EquipmentAll` og `EquipmentReadSub` som permissions. I service-laget så sjekker vi tilattelsen manuelt ved å kalle på `DepartmentScopeService` i Create, Update og Delete-metodene.

**Kompetanser** — `CompetencyRepository` har tilsvarende `ApplyDepartmentFilter` som sjekker `CompetenciesAll` og `CompetenciesReadSub`. I service-laget så sjekker vi tilattelsen manuelt ved å kalle på `DepartmentScopeService` i Create, Update og Delete-metodene.

**Dokumenter** — dokumenter har en annerledes tilgangsmodell. De er ikke eid av en avdeling, men publisert *til* avdelinger og stillingstitler. `DocumentRepository.ApplyTargetingFilter` håndterer dette ved å filtrere på hvilke avdelinger og stillingstitler dokumentet er rettet mot. Dette er publiseringslogikk, ikke scope-logikk, og styres av kalleren. MÅ TESTES MER

### `IgnoreQueryFilters()` brukes bevisst flere steder

Vi måtte skru av filteret for noen tilfeller.

- **Auth-flyten** (`GetByEmailAsync`, `GetByIdIgnoringFiltersAsync`) — en bruker som ikke er logget inn ennå har ingen avdeling i scope, og kan dermed ikke finne seg selv for å logge inn
- **OtpCode og RefreshToken** — auth-entiteter som aldri skal scope-filtreres
- **`DepartmentRepository.GetAllWithHierarchyAsync`** — brukes av BFS-søket internt for å bygge hierarkiet. Hvis dette filteret var på ville BFS prøve å kalle seg selv rekursivt og krasje
- **`DepartmentRepository.HasMembersAsync`** — må se alle brukere i avdelingen uavhengig av scope, ellers kan avdelinger med medlemmer utenfor scope feilaktig slettes
- **`EquipmentIssuanceRepository.ApplyDepartmentFilter`** — subquery mot `AspNetUsers` bruker `IgnoreQueryFilters` for å unngå rekursivt filter, med manuell `DeletedAt == null`-sjekk
- **Seeding og dev-kontrollere** — trenger tilgang til alle brukere uavhengig av avdeling
- **`IssuedBy`/`Uploader`-navigasjoner** — løst ved å konfigurere relasjonene som optional (`IsRequired(false)`) slik at EF bruker LEFT JOIN i stedet for INNER JOIN, og brukere i andre avdelinger ikke filtrerer bort dokumenter/utstyr de har opprettet