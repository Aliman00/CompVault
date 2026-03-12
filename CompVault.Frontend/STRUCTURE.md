# Blazor Server Frontend – Mappestruktur

Prosjektet følger **ByFeature**/**Vertical Slice Architecture** – kode som hører sammen, ligger sammen.
Hver feature er selvforsynt med sin egen klient, modeller og eventuelle services.

---

## Rotnivå

```
CompVault.Frontend/
├── Common/
├── Extensions/
├── Features/
├── wwwroot/
├── _Imports.razor
├── App.razor
├── appsettings.json
├── appsettings.Development.json
├── Program.cs
└── Routes.razor
```

---

## `Common/`

Alt som er globalt og deles på tvers av features.
Ingenting her skal vite om en spesifikk feature.

### Eksempel bruk:

```
Common/
├── Components/
│   ├── LoadingSpinner.razor        # Generisk laste-indikator
│   ├── ConfirmDialog.razor         # Gjenbrukbar bekreftelsesdialog
│   └── EmptyState.razor            # Vises når lister er tomme
│
├── Layouts/
│   ├── MainLayout.razor            # Hoved-layout med sidebar/navbar
│   ├── AuthLayout.razor            # Minimalt layout for innloggingssider
│   ├── Error.razor                 # Vises ved ubehandlede exceptions
│   └── NotFound.razor              # Vises når ingen route matcher
│
├── Forms/
│   ├── AppInputText.razor          # Wrapper rundt standard InputText med felles styling
│   └── AppValidationSummary.razor  # Felles valideringsvisning
│
└── Services/
    ├── AuthStateProvider.cs        # Holder autentiseringstilstand for hele appen
    ├── ToastService.cs             # Varsler/notifikasjoner brukt overalt
    └── ThemeService.cs             # Mørkt/lyst tema – global UI-state
```

**Tommelfingerregel for Services:** Spør deg selv – "brukes dette i mer enn én feature?"
- Ja → legg det i `Common/Services/`
- Nei → legg det inne i featuren

---

## `Features/`

Hoveddelen av prosjektet. Én mappe per domeneområde.
Ingen feature skal importere fra en annen features mappe.

### Eksempel bruk:

```
Features/
├── Users/
│   ├── UserList.razor              # UI-komponent
│   ├── UserList.razor.cs           # Code-behind (logikk separert fra markup)
│   ├── UserDetail.razor
│   ├── UserDetail.razor.cs
│   ├── UserApiClient.cs            # HTTP-kall mot backend – kun Users-endepunkter
│   └── UserViewModel.cs            # ViewModel/DTO kun for denne featuren
│
├── Auth/
│   ├── Login.razor
│   ├── Login.razor.cs
│   ├── AuthApiClient.cs            # Kaller /auth/* på backend
│   └── LoginModel.cs
│
├── Documents/
│   ├── DocumentList.razor
│   ├── DocumentList.razor.cs
│   ├── DocumentApiClient.cs
│   ├── DocumentFilterService.cs    # Feature-lokal service – kun Documents bryr seg om dette
│   └── DocumentViewModel.cs
│
└── Dashboard/
    ├── Dashboard.razor
    ├── Dashboard.razor.cs
    └── DashboardApiClient.cs
```

**Regler:**
- `ApiClient` har ansvar for HTTP – komponenten kaller `ApiClient`, ikke `HttpClient` direkte
- `ViewModel` er UI-representasjon – ikke det samme som backend-DTO
- Legg til en feature-lokal `*Service.cs` hvis featuren trenger intern state-håndtering

---



## `Extensions/`

DI-registrering. `Program.cs` skal være så ren som mulig.

### Eksempel bruk:

```
Extensions/
└── ServiceCollectionExtensions.cs  # Registrerer alle typed HttpClients, services osv.
```

```csharp
// Program.cs
builder.Services.AddWebServices(builder.Configuration);

// ServiceCollectionExtensions.cs
public static IServiceCollection AddWebServices(this IServiceCollection services, IConfiguration config)
{
    services.AddScoped<ToastService>();
    services.AddScoped<AuthStateProvider>();

    services.AddHttpClient<UserApiClient>(client =>
        client.BaseAddress = new Uri(config["ApiBaseUrl"]!));

    services.AddHttpClient<AuthApiClient>(client =>
        client.BaseAddress = new Uri(config["ApiBaseUrl"]!));

    return services;
}
```

---

## `wwwroot/`

Statiske filer. Ingen C#-logikk her.

### Eksempel bruk:

```
wwwroot/
├── css/
│   ├── app.css                     # Global CSS / CSS-variabler
│   └── features/
│       └── users.css               # Feature-spesifikk CSS (valgfritt)
├── js/
│   └── interop.js                  # JavaScript Interop-funksjoner
└── images/
    └── logo.svg
```

---

## Avhengighetsregler (oppsummert)

```
Komponent (.razor)
    └── kaller ApiClient
            └── bruker HttpClient → Backend API

Komponent
    └── kan bruke global Service (ToastService, AuthStateProvider)
    └── kan bruke feature-lokal Service (DocumentFilterService)

Feature A
    └── skal IKKE importere fra Feature B
```

---

## Eksempel: komplett feature fra A til Å

```csharp
// Features/Users/UserApiClient.cs
public class UserApiClient(HttpClient http)
{
    public async Task<List<UserViewModel>> GetUsersAsync()
    {
        return await http.GetFromJsonAsync<List<UserViewModel>>("users") ?? [];
    }
}

// Features/Users/UserViewModel.cs
public record UserViewModel(Guid Id, string FullName, string Email);

// Features/Users/UserList.razor.cs
public partial class UserList : ComponentBase
{
    [Inject] private UserApiClient ApiClient { get; set; } = default!;
    [Inject] private ToastService Toast { get; set; } = default!;

    private List<UserViewModel> _users = [];

    protected override async Task OnInitializedAsync()
    {
        _users = await ApiClient.GetUsersAsync();
    }
}
```

```razor
@* Features/Users/UserList.razor *@
@page "/users"
@inherits UserList

<h1>Brukere</h1>

@foreach (var user in _users)
{
    <p>@user.FullName</p>
}
```