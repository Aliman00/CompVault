# Extensions

Her samles oppstartskode som ellers fort gjør `Program.cs` unødvendig lang. DI-registrering, autentiseringsoppsett og konfigurasjon av HttpClient ligger her.

## Struktur

```text
Extensions/
├── ServiceCollectionExtensions.cs    <- DI-registrering av services, API-klienter, autentisering
└── WebApplicationBuilderExtensions.cs <- Konfigurasjon av logging og annet bygger-oppsett
```

## Hva registreres?

`ServiceCollectionExtensions.cs` deler opp i metoder per ansvarsområde:

- **HttpClient-oppsett** — konfigurerer base-URL og autentiseringshandler
- **Autentisering** — cookie-basert auth med JWT-validering og token-oppfrisking
- **Frontend-tjenester** — app-spesifikke services som brukes av features
- **MudBlazor** — UI-komponent-biblioteket

`WebApplicationBuilderExtensions.cs` tar seg av logging og annet som hører til `WebApplicationBuilder`.

## Når du registrerer noe nytt

1. Finn riktig metode i `ServiceCollectionExtensions.cs` (eller lag en ny hvis det trengs).
2. Registrer med `AddScoped`, `AddSingleton` eller `AddHttpClient` avhengig av levetid.
3. Kall metoden fra `Program.cs`.

**Retningslinje:** `AddScoped` er som regel riktig for services og API-klienter. `AddSingleton` passer for konfigurasjonsobjekter og state-less tjenester.
