# Infrastructure

`Infrastructure/` samler det som snakker med ting utenfor selve domenelogikken. Hos oss betyr det først og fremst database, autentisering, e-post og annen teknisk integrasjon som backend trenger for å fungere.

Det viktigste skillet her er at EF Core og andre eksterne avhengigheter holdes samlet i dette laget, i stedet for å sive utover i resten av prosjektet.

## Struktur

```text
Infrastructure/
├── <Kategori>/       <- f.eks. Data, Auth, Email, Extensions, FileStorage, Jobs, Repositories
├── <Kategori>/
└── <Kategori>/       <- ny kategori ved behov
```

Hver undermappe har som regel sin egen README som forklarer hva som ligger der og hvordan det brukes.

## Retningslinjer

- Feature-kode bør gå via repositories og services, ikke bruke `AppDbContext` eller EF Core direkte. Unntak finnes for interceptorer og enkelte services der komplekse spørringer gjør det upraktisk å gå via repository.
- Repositories skal håndtere dataaksess og change tracking — forretningslogikk hører hjemme i service-laget.
- Nye eksterne tjenester legges i egne undermapper under `Infrastructure/` når det gir mening.
