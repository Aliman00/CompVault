# Frontend — tekniske ting

Dette er en oppsummering av frontend-delen. Selv om fokuset har vært på backenden, er frontend ikke bare et skall. Den har egen modulstruktur, gjenbrukbare komponenter, og service-lag.

---

## 1. Samme modulstruktur som backend

Frontend er organisert i samme feature-moduler som backend:

```
Features/
├── Auth/
│   ├── Pages/        ← LoginEmail.razor, LoginOtp.razor
│   └── Services/     ← AuthService, IAuthService
├── Users/
│   ├── Pages/        ← UserList, UserCreate, UserDetail, ProfilePage
│   ├── Components/   ← UserDocumentTab, UserCompetencyTab, etc.
│   └── Services/     ← UserService, IUserService
└── ... (10 moduler totalt)
```

Hver modul har egne `Pages`, `Components`, og `Services`. Så hvis du jobber med "brukere", finner du alt under `Features/Users/`.

---

## 2. Gjenbrukbare komponenter

I stedet for å kopiere MudBlazor-kode inn i hver side, har vi bygget **27 gjenbrukbare komponenter** i `Common/Components/`:

- **Felter:** TextField, NumberField, SelectWithLinkField, MultiSelectField, EnumField, FileUploadField, UserAutocomplete
- **Skjema:** CreateForm, DetailForm
- **Tabeller:** ListPage, UserTable
- **Dialoger:** ConfirmDeleteDialog
- **Knapper:** ResponsiveButton, ToggleButton
- **Header:** ListHeader, DetailHeader
- **Layout:** AppBar, AppDrawer, AppNavMenu, MainLayout, AuthLayout
- **Annet:** AppAlert, AppSpinner, AppLogo, UserProfileDrawer

**Eksempel:** `CreateForm` brukes på tvers av moduler for opprettelsesskjemaer. Den tar inn en modell, valideringsregler, og en callback — og håndterer visning, lagring, feilmeldinger og suksess-alert internt. Modulene slipper å skrive skjemalogikk fra scratch.

---

## 3. Service-lag med JWT-håndtering

Frontend har **39 service-klasser** som snakker med backenden. Hver modul har egne `I<Service>` + `<Service>`-par:

- `IAuthService` / `AuthService` — login, OTP, token-lagring, logout
- `IUserService` / `UserService` — CRUD mot `api/users`
- `IDocumentService` / `DocumentService` — dokumenter med filopplasting
- `ISignatureService` / `SignatureService` — signering

JWT lagres i minnet (ikke localStorage av sikkerhetshensyn) og injectes i `Authorization`-header på alle HTTP-kall. Hvis tokenet utløper, redirectes brukeren til login.

---

## 4. Responsivt UI med MudBlazor

Vi bruker **MudBlazor 9.2.0** — et Material Design-komponentbibliotek for Blazor.

- **Responsivt layout:** MainLayout med AppBar (top) + AppDrawer (side). Draweren skjules automatisk på små skjermer.
- **Tema:** Egendefinert tema-fil i `Common/Themes/` som overstyrer standardfarger.
- **Lokaliseringsstøtte:** `Common/Localization/` med norske tekster.

---

## 5. Side-basert autorisasjon

Blazor-sider bruker `@attribute [Authorize(Policy = "users:read")]` direkte på Razor-sidene. Hvis brukeren mangler permission, vises `NotAuthorized`-layouten.

Dette er deklarativ autorisasjon — man ser rett på siden hva som kreves.

---

## 6. Faner og dialoger

Mange sider bruker faner for å vise relatert data:

- **UserDetail** har faner: Profil, Kompetanser, Dokumenter, Utstyr, Audit
- **DepartmentDetail** har faner: Info, Medlemmer, Underavdelinger
- **DocumentTypeDetail** har faner: Info, Dokumenter, Kategorier

Dialoger brukes for handlinger som krever bekreftelse:
- **ConfirmDeleteDialog** — gjenbrukbar "er du sikker?"
- **AssignRoleDialog** — tildel roller til bruker
- **ConfirmRevokeDialog** — tilbakekall kompetanse

---

## 7. Filopplasting

`DocumentService` og `FileUploadField` håndterer filopplasting med:
- MIME-type-validering (basert på `DocumentType.AllowedMimeTypes`)
- Maks filstørrelse (fra `DocumentType.MaxFileSizeBytes`)
- SHA256-sjekksum (verifisert både frontend og backend)

---

## 8. Feilhåndtering

- **ReconnectModal** — vises hvis SignalR/Blazor-tilkoblingen brytes
- **Error** — global feilhåndtering (ErrorBoundary)
- **NotFound** — 404-side
- **NotAuthorized** — "Du har ikke tilgang til denne siden"

---

Så frontend er ikke bare et skall. Den har modulstruktur, gjenbrukbare komponenter, JWT-håndtering, responsivt UI, og feilhåndtering. Det er et frontend som er vedlikeholdbart — ikke en samling kopiert HTML.
