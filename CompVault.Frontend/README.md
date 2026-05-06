# CompVault.Frontend

Dette er frontend-delen av CompVault — et Blazor Server-prosjekt med MudBlazor som UI-rammeverk. Appen snakker med backend over HTTP og bruker server-side rendering med interaktive komponenter.

## Hva er dette?

CompVault er et system for kompetanse- og samsvarsstyring. Frontend er én del av et tolags-prosjekt:

- **CompVault.Backend** — ASP.NET Core Web API med PostgreSQL
- **CompVault.Frontend** — Blazor Server-app (dette prosjektet)
- **CompVault.Shared** — Delt kontraktsbibliotek med DTO-er, enums og konstanter

## Teknologier

- **Blazor Server** — server-side rendering med SignalR for interaktivitet
- **MudBlazor** — Material Design-komponenter
- **Serilog** — strukturert logging
- **Cookie-basert autentisering** — JWT-token lagres i HttpOnly-cookie

## Struktur

```text
CompVault.Frontend/
├── Common/         ← ting som deles på tvers av features (layouts, komponenter, tjenester)
├── Extensions/     ← DI-registrering og oppstartshjelpere
├── Features/       ← én mappe per fagområde (Users, Documents, Auth, osv.)
├── wwwroot/        ← statiske filer (CSS, JS, bilder)
├── _Imports.razor  ← globale using-direktiver
├── App.razor       ← rot-komponenten
└── Program.cs      ← oppstart
```

Mappen `Features/` følger samme filosofi som backend: kode som hører sammen, ligger sammen. Hver feature har sine egne sider, komponenter og tjenester.

For detaljer om mappestruktur og arkitektur, se `STRUCTURE.md`.
