# Frontend-dokumentasjon

Her ligger oversikten over frontend-delen av CompVault.

---

## Hva er frontend?

Frontend er en **Blazor Server-app** med MudBlazor som UI-rammeverk. Den snakker med backend over HTTP-kall og bruker server-side rendering med interaktive komponenter.

**Teknologier:**
- Blazor Server (server-side rendering + SignalR)
- MudBlazor 9.2.0 (Material Design-komponenter)
- JWT som lagres i minnet (ikke localStorage)
- Cookie-basert autentisering med HttpOnly-cookie

**Hvorfor Blazor:** Vi ville ha C# på begge sider — backend og frontend. Da slipper vi å bytte språk og kan dele modeller via `CompVault.Shared`.

---

## Moduler

Frontend er delt i samme feature-moduler som backend. Hver modul har egne `Pages/`, `Components/` og `Services/`.

| Modul | Hva den gjør | Viktige sider |
|-------|-------------|---------------|
| **Auth** | Innlogging via e-post + OTP. Håndterer JWT-tokens og logout. | `LoginEmail`, `LoginOtp` |
| **Users** | Brukerliste, opprett bruker, rediger profil, se detaljer. Tabs for kompetanser, dokumenter, utstyr og audit. | `UserList`, `UserCreate`, `UserDetail`, `ProfilePage` |
| **Roles** | Rolleliste, opprett rolle, tildel permissions. | `RoleList`, `RoleCreate`, `RoleDetail` |
| **Departments** | Avdelingsliste, avdelingshierarki (tre-visning), opprett/endre avdeling. | `DepartmentList`, `DepartmentDetail`, `DepartmentCreate` |
| **JobTitles** | Stillingstitler. | `JobTitleList`, `JobTitleCreate` |
| **Competencies** | Kompetanseliste, opprett/endre kompetanse, kompetansetyper. Varsler om utløpskandidater. | `CompetencyList`, `CompetencyCreate`, `CompetencyTypeList` |
| **Documents** | Dokumenttyper, kategorier, dokumentliste, opplasting, signering, nedlasting. | `DocumentsOverview`, `DocumentTypeDetail`, `DocumentUpload` |
| **Equipment** | Utstyrs-kategorier, items, utleveringer. "My Equipment" for den innloggede brukeren. | `EquipmentList`, `EquipmentIssuanceList`, `MyEquipment` |
| **Audit** | Se audit-logg for hvem som gjorde hva og når. | `AuditLog` |
| **Dashboard** | Hovedside etter innlogging. Viser relevant info avhengig av rolle. | `Dashboard` |

---

## Gjenbrukbare komponenter

I stedet for å kopiere MudBlazor-kode inn i hver side, har vi bygget **27 felles komponenter** i `CompVault.Frontend/Common/Components/`:

- **Felter** (`Fields/`) — TextField, NumberField, SelectWithLinkField, MultiSelectField, EnumField, FileUploadField, UserAutocomplete
- **Skjema** (`Forms/`) — CreateForm, DetailForm
- **Tabeller** (`Tables/`) — ListPage, UserTable
- **Dialoger** (`Dialogs/`) — ConfirmDeleteDialog
- **Knapper** (`Buttons/`) — ResponsiveButton, ToggleButton
- **Header** (`Headers/`) — ListHeader, DetailHeader
- **Layout** — AppBar, AppDrawer, AppNavMenu, MainLayout, AuthLayout
- **Annet** — AppAlert, AppSpinner, AppLogo, UserProfileDrawer

Tanken er: hvis noe brukes i mer enn én feature, ligger det i `Common/`. Alt annet ligger inne i feature-mappen.

---

## Flytdiagrammer

- **`docs/flytdiagram/Frontend/`** — Flyten gjennom frontend: innlogging (e-post → OTP → JWT), logout, token-refresh, claims-refresh, cookie-validering.
- **`docs/flytdiagram/Backend/`** — Avdelings-scoping i backend: hvordan frontendens request filtreres basert på brukerens rolle og avdelingstilhørighet.

---

## Mappestruktur

```
CompVault.Frontend/
├── Common/
│   ├── Components/     ← gjenbrukbare komponenter
│   ├── Layouts/        ← MainLayout, AuthLayout, feil-sider
│   ├── Services/       ← autentisering, tema, toast-varsler
│   └── ...
├── Features/
│   ├── Users/
│   │   ├── Pages/      ← routable .razor-sider
│   │   ├── Components/ ← interne komponenter
│   │   ├── Services/   ← API-klient mot backend
│   │   └── Models/     ← view-modeller
│   └── ... (alle moduler)
├── Extensions/         ← DI-registrering
├── wwwroot/           ← statiske filer (CSS, JS, bilder)
├── _Imports.razor     ← globale using-direktiver
├── App.razor          ← rot-komponent
└── Program.cs         ← oppstart
```

---

## JWT-håndtering

1. Bruker logger inn med e-post → backend sender OTP.
2. Bruker taster inn OTP → backend returnerer JWT access token.
3. Tokenet lagres i minnet via `AuthService`.
4. Alle HTTP-kall injekterer tokenet i `Authorization`-header.
5. Hvis tokenet utløper (15 min), redirectes brukeren til login.
6. Logout = slett token + kall `api/auth/revoke` + redirect til login.

---

## Hva frontend IKKE gjør

- Den har **ingen egen database** — all data kommer fra backend over HTTP.
- Den har **ingen egen autorisasjonslogikk** — backenden sjekker permissions og returnerer 403 hvis brukeren mangler tilgang.
- Den har **ingen egen soft delete-logikk** — det håndteres i backend.

---

For detaljer om en spesifikk modul, se `CompVault.Frontend/Features/<Modul>/`.
For mer om arkitekturen, se `CompVault.Frontend/README.md`.
