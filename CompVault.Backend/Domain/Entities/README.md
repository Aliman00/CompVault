# Domain / Entities

`Domain/Entities/` inneholder entitetsklassene som beskriver de viktigste modellene i systemet. Hos oss prøver vi å holde disse klassene så rene som mulig, uten å blande inn EF Core, web-lag eller annen teknisk infrastruktur direkte i dem.

## Struktur

```text
Domain/Entities/
├── <Domene1>/       <- f.eks. Audit, Auth, Documents, Equipment, JobTitles, Notifications
├── <Domene2>/
└── <Domene>/
```

## Hvordan vi organiserer entiteter

Entiteter er gruppert etter domeneområde, ikke etter teknisk type. Det betyr at modeller som hører til samme fagområde ligger samlet i samme mappe, i stedet for å være spredt rundt bare fordi de er "entities".

Det gjør det lettere å se hvilke modeller som faktisk hører sammen. Når man jobber med ett fagområde, er det greit å finne relaterte entiteter på samme sted.

## Retningslinjer

- Entiteter bør ikke ha direkte avhengigheter til EF Core, ASP.NET eller andre rammeverk. Identity-entiteter (`ApplicationUser`, `ApplicationRole`) er et unntak her — de må nødvendigvis arve fra `IdentityUser<Guid>` og `IdentityRole<Guid>`.
- Entiteter bør heller ikke kjenne til services, repositories eller DTO-er fra andre lag.
- Enkel logikk som naturlig hører til modellen kan være grei å ha her, for eksempel beregnede properties som `FullName` eller `IsValid`.
- Enums legges i `CompVault.Shared/Enums/` slik at de kan brukes både i backend og frontend.

Poenget er ikke å gjøre entitetene kunstig tomme, men å unngå at de drar med seg masse teknisk ansvar de ikke trenger å ha.

## Når du legger til en ny entitet

1. Finn riktig domene-mappe, eller opprett en ny hvis den ikke finnes.
2. Legg entiteten i `Domain/Entities/<Domene>/`.
3. Opprett tilhørende EF Core-konfigurasjon i `Infrastructure/Data/Configurations/<Domene>/`.

Hvis en modell begynner å få mye teknisk logikk rundt seg, er det ofte et tegn på at noe av det bør flyttes ut til andre lag i stedet for å bli liggende i entiteten.
