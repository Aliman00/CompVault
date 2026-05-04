# Tilgangskontrollmodulen (RBAC) — Moduldokumentasjon

RBAC-modulen styrer hvem som får lov til hva i CompVault. Her var målet å få på plass et system med roller og permissions som er fleksibelt nok til å brukes på tvers av hele systemet, men uten at det blir mer komplisert enn nødvendig.

## 1. Problemstilling og behov

Utgangspunktet for modulen var:
> Hvordan kan vi sikre at kun autoriserte brukere får tilgang til riktige deler av systemet, og at dette er fleksibelt nok til å tilpasses bedriftens behov?

Konkrete krav til løsningen:
- Kunne definere roller med navn og beskrivelse, slik at bedriften kan modellere sine egne tilgangsnivåer.
- Tildele finmaskede permissions til rollene, basert på et `resource:action`-mønster (f.eks. `users:read`, `roles:write`).
- Koble brukere til roller via ASP.NET Identity sin innebygde `AspNetUserRoles`-tabell.
- Beskytte systemroller mot sletting og navneendring via `IsSystem`-flagget (beskyttelsen er implementert, men ingen roller seedes med `IsSystem = true` — se utfordringer).
- Bake permissions inn i JWT-tokenet, slik at autorisasjon kan sjekkes uten ekstra databaseoppslag.
- Registrere policyer dynamisk — nye permissions skal automatisk bli tilgjengelige uten manuell konfigurasjon.

## 2. Teknisk design

### Datamodell

Modulen har fire entiteter: `ApplicationRole`, `Permission`, `RolePermission` (koblingstabell), og `ApplicationUser` — sistnevnte eies av Users-modulen men rollene administreres her. Den fullstendige datamodellen er dokumentert i `rbac-er-diagram.pdf`.

**ApplicationRole:**
| Felt | Type | Hva det er |
|------|------|------------|
| `Id` | Guid | Primærnøkkel (arves fra `IdentityRole<Guid>`) |
| `Name` | varchar(256) | Rollenavn, f.eks. "Admin" |
| `Description` | varchar(250) | Kort forklaring av hva rollen innebærer |
| `IsSystem` | bool | Systemroller kan ikke slettes eller omdøpes |
| `CreatedAt` | DateTime | Når rollen ble opprettet |
| `CreatedById` | Guid? | Hvem som opprettet rollen |

**Permission:**
| Felt | Type | Hva det er |
|------|------|------------|
| `Id` | Guid | Primærnøkkel |
| `Name` | varchar(100) | Unikt navn, f.eks. "users:read" |
| `Description` | varchar(500) | Hva tillatelsen gir tilgang til |
| `Category` | varchar(100) | Hvilken modul den tilhører |

**RolePermission:**
| Felt | Type | Hva det er |
|------|------|------------|
| `RoleId` | Guid | FK til ApplicationRole (sammensatt PK) |
| `PermissionId` | Guid | FK til Permission (sammensatt PK) |
| `GrantedAt` | DateTime | Når tillatelsen ble tildelt |
| `GrantedById` | Guid? | Hvem som tildelte |

**Permissions (37 totalt):**
- Users (5): `users:read`, `users:write`, `users:delete`, `users:read:all`, `users:read:sub`
- Roles (3): `roles:read`, `roles:write`, `roles:delete`
- Departments (5): `departments:read`, `departments:write`, `departments:delete`, `departments:read:all`, `departments:read:sub`
- Competencies (5): `competencies:read`, `competencies:write`, `competencies:delete`, `competencies:read:all`, `competencies:read:sub`
- DocumentTypes (3): `document_types:read`, `document_types:write`, `document_types:delete`
- Documents (6): `documents:read`, `documents:write`, `documents:delete`, `documents:sign`, `documents:all:departments`, `documents:read:sub`
- JobTitles (3): `job_titles:read`, `job_titles:write`, `job_titles:delete`
- Equipment (5): `equipment:read`, `equipment:write`, `equipment:delete`, `equipment:read:all`, `equipment:read:sub`
- Admins (1): `admin:access`
- Audit (1): `audit:read`

### Autorisasjonsflyt

Autorisasjon følger en ganske enkel kjede:

1. **Innlogging** — `AuthService` henter brukerens roller og slår opp permissions via `PermissionService` (som kaller `RoleRepository.GetPermissionNamesForRolesAsync`).
2. **Token-generering** — `JwtService` baker rolle- og permission-claims direkte inn i JWT-tokenet. Ingen nytt databaseoppslag på hvert API-kall.
3. **API-kall** — ASP.NET Core sjekker tokenet mot registrerte policyer. Hver permission er registrert som en egen policy som krever et `permission`-claim med riktig verdi.
4. **Policy-registrering** — Skjer dynamisk via refleksjon: `Permissions`-klassen leses, og hvert `public static string`-felt blir en egen policy.

### Arkitektur

`RoleService` er navet i modulen og bruker både ASP.NET Identity (`RoleManager`, `UserManager`) og `RoleRepository` for databaseoperasjoner. `PermissionService` er en tynn wrapper rundt `RoleRepository` og brukes av `AuthService` under innlogging og token-refresh for å slå opp permissions på rollene brukeren har. Samspillet er vist i `rbac-arkitektur.png`.

**Komponentoversikt:**

| Komponent | Type | Ansvar |
|-----------|------|--------|
| `RolesController` | Controller | CRUD for roller, permissions-tildeling, hent alle permissions. `roles:read/write/delete`. |
| `RoleService` | Service | Navneunikhet, systemrolle-beskyttelse, atomisk permission-tildeling (UnitOfWork), brukerantall-sjekk ved sletting. |
| `RoleMapper` | Statisk klasse | `ApplicationRole → RoleDto` (inkl. UserCount, IsSystem, CreatedByName, Permissions-liste). `Permission → PermissionDto`. |
| `RoleRepository` | Repository | `GetAllWithPermissionsAsync`, `GetByIdWithCreatedByAsync`, `GetUserCountsForRolesAsync`, `GetPermissionNamesForRoleAsync`, `GetPermissionNamesForRolesAsync`, `GetPermissionsByNamesAsync`, `RemoveRolePermissionsAsync`, `AddRolePermissionsAsync`, `GetAllPermissionsAsync`. |
| `PermissionService` | Service | Tynn wrapper: `GetPermissionsForRolesAsync` → `RoleRepository.GetPermissionNamesForRolesAsync`. Brukes av `AuthService`. |
| `JwtService` | Service | Baker permissions (mottatt som parameter) inn i JWT-tokenet. |

### Designvalg

| Valg | Hvorfor |
|------|--------|
| **Permission-basert, ikke bare roller** | Roller alene er for grovmasket. Med `resource:action`-permissions kan vi gi en rolle lesetilgang til brukere uten skrivetilgang til roller. |
| **Claims i JWT** | Slipper databaseoppslag på hvert API-kall. Ulempe: endringer i permissions slår ikke inn før nytt token. OK med 15 min access token-levetid. |
| **Dynamisk policy-registrering** | Refleksjon over `Permissions`-klassen. Ny permission = ny konstant. Resten skjer automatisk. |
| **Systemroller med IsSystem** | Admin og Employee er beskyttet via `IsSystem`-flagget i koden (`RoleService` nekter sletting og omdøping), men per nå seedes ingen roller med `IsSystem = true` — `RoleSeeder.cs` setter alle til `false`. Beskyttelsen fungerer, men er kun aktivert i testene. |
| **Atomisk permission-tildeling** | `UnitOfWork.ExecuteInTransactionAsync` — fjerner gamle og legger til nye i én operasjon. Umulig å havne i delvis oppdatert tilstand. |
| **Bulk brukerantall** | `GetUserCountsForRolesAsync` med `GroupBy` — unngår N+1 når alle roller skal vises med antall tilknyttede brukere. |
| **Audit-sporing ved permission-endring** | `AssignPermissionsAsync` skriver en manuell `AuditLog`-entry med hva som ble lagt til og fjernet, inkludert brukernavn og e-post. |
| **GrantedById med OnDelete: Restrict** | Brukeren som tildelte permissions kan ikke slettes — sikrer full sporbarhet. |

## 3. Implementasjon

`RoleService` er den sentrale brikken. `GetAllAsync` henter alle roller med permissions inkludert, og slår opp brukerantall i én bulk-spørring via `GetUserCountsForRolesAsync` — uten dette ville vi fått N+1 på visningen. `GetByIdAsync` gjør det samme for én rolle.

`CreateAsync` bruker `roleManager.RoleExistsAsync()` for å sjekke navneunikhet, og `userManager.FindByIdAsync()` for å verifisere at brukeren som oppretter finnes. Nye roller får alltid `IsSystem = false`. `UpdateAsync` bruker `roleManager` direkte, men legger til beskyttelse: systemroller kan ikke omdøpes, og navnekonflikter sjekkes med `roleManager.RoleExistsAsync()`. `DeleteAsync` nekter sletting av systemroller og roller som har brukere tilknyttet.

`AssignPermissionsAsync` er den mest komplekse metoden. Den validerer at alle permissions som sendes inn faktisk finnes i databasen, og kjører deretter hele operasjonen i en transaksjon via `UnitOfWork`: fjern alle eksisterende permissions, legg til de nye, og skriv en manuell `AuditLog`-entry med detaljer om hva som ble lagt til og fjernet. Dette er det eneste stedet i prosjektet der `UnitOfWork` brukes — vi over-abstraherer ikke, men tar det i bruk der det faktisk trengs.

`RoleRepository` er en større klasse med mange metoder, men de er alle ganske rett fram. `GetAllWithPermissionsAsync` bruker `Include` + `ThenInclude` for å laste hele treet: rolle → RolePermission → Permission i én spørring. `GetUserCountsForRolesAsync` kjører en `GroupBy` mot Identity sin `UserRoles`-tabell. `GetPermissionNamesForRolesAsync` brukes av `PermissionService` under innlogging — den slår opp permissions for flere roller samtidig via `Contains` på rollenavn.

`PermissionService` er en tynn wrapper — én metode, `GetPermissionsForRolesAsync`, som bare delegerer videre til `RoleRepository.GetPermissionNamesForRolesAsync`. Den eksisterer mest for å holde `AuthService` isolert fra repository-laget.

Policy-registrering skjer i `ServiceCollectionExtensions.AddAuth()` via refleksjon over `Permissions`-klassen, og `JwtService` baker alt inn i tokenet. Disse delene er dokumentert i auth-modulen siden det er der flyten starter.

## 4. Utfordringer og beslutninger

### ASP.NET Identity + eget permission-system

Identity dekker brukere og roller ferdig ut av boksen, men har null støtte for finmaskede permissions. Vi kunne bygd hele greia selv, men det hadde vært bortkastet — Identity sin `RoleManager` og `UserManager` fungerer allerede. I stedet la vi permission-systemet oppå. `Permission` og `RolePermission` er våre egne tabeller, mens bruker-rolle-koblingen går gjennom Identity sin standard `AspNetUserRoles`-tabell. Samlingspunktet for disse to systemene er `AuthService`, som henter roller via `UserManager` og permissions via `PermissionService`, for så å sende alt samlet til `JwtService`.

### Atomisk permission-tildeling

Å endre permissions på en rolle er egentlig to operasjoner: slett gamle, legg til nye. Hvis noe kræsjer mellom disse stegene, står rollen der med ingen permissions — eller feil permissions. `AssignPermissionsAsync` pakker begge operasjonene inn i en `UnitOfWork`-transaksjon. Enten skjer alt, eller ingenting. Dette er det eneste stedet i hele prosjektet vi faktisk trenger `UnitOfWork` — og det er helt bevisst.

### Systemroller som sikkerhetsnett

En av de første tingene vi tenkte på var: hva hvis noen sletter "Admin"-rollen ved et uhell? Da er alle låst ute. Løsningen er `IsSystem`-flagget og beskyttelsen i `RoleService`. Interessant nok seedes ingen roller med `IsSystem = true` — `RoleSeeder.cs` setter alle til `false`. Beskyttelsen mot sletting og omdøping er implementert og testet, men den er ikke aktivert i seed-data. Dette betyr at rollene i praksis er beskyttet av koden i `UpdateAsync` og `DeleteAsync`, men kun hvis noen manuelt setter `IsSystem = true` i databasen.

### Permission-endringer og token-levetid

Siden permissions bakes inn i tokenet ved innlogging, vil ikke endringer slå inn for allerede innloggede brukere før de får nytt token. Vi vurderte å ugyldiggjøre tokens ved permission-endring, men det ble for mye kompleksitet for dette prosjektet. 15 minutters access token-levetid begrenser uansett vinduet.

## 5. Vurdering og refleksjon

*(Denne seksjonen fylles ut senere.)*

## 6. Relaterte moduler

| Modul | Relasjon |
|-------|----------|
| **Auth** | `JwtService` baker permissions inn i JWT; `PermissionService` kalles under innlogging |
| **Users** | `ApplicationUser` har roller via `AspNetUserRoles`; `RolePermission.GrantedById` og `ApplicationRole.CreatedById` peker på brukere |
| **Departments** | Permissions: `departments:read/write/delete`, `departments:read:all`, `departments:read:sub` |
| **Competencies** | Permissions: `competencies:read/write/delete`, `competencies:read:all`, `competencies:read:sub` |
| **Documents** | Permissions: `documents:read/write/delete/sign`, `documents:all:departments`, `documents:read:sub`, `document_types:read/write/delete` |
| **JobTitles** | Permissions: `job_titles:read/write/delete` |
| **Equipment** | Permissions: `equipment:read/write/delete`, `equipment:read:all`, `equipment:read:sub` |
