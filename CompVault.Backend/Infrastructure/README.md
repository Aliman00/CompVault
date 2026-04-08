# Infrastructure

`Infrastructure/` samler det som snakker med ting utenfor selve domenelogikken. Hos oss betyr det først og fremst database, autentisering, e-post og annen teknisk integrasjon som backend trenger for å fungere.

Det viktigste skillet her er at EF Core og andre eksterne avhengigheter holdes samlet i dette laget, i stedet for å sive utover i resten av prosjektet.

## Struktur

```text
Infrastructure/
├── Data/              <- AppDbContext, IUnitOfWork, EF-konfigurasjoner
├── Auth/              <- JWT-tjenester og innstillinger
├── Email/             <- E-posttjeneste, maler og konfigurasjon
├── Repositories/      <- Generisk repository-base (IRepository<T>, BaseRepository<T>)
├── Jobs/              <- Bakgrunnsjobber
└── Extensions/        <- DI-registrering og middleware-oppsett
```

## Retningslinjer

- Kode utenfor `Infrastructure` bør ikke importere EF Core-navnerom direkte.
- `AppDbContext` brukes i utgangspunktet bare fra `Infrastructure` og eventuelt i entrypointet (`Program.cs`).
- Nye eksterne tjenester legges i egne undermapper under `Infrastructure/` når det gir mening.
