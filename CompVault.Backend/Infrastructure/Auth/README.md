# Infrastructure/Auth

`Infrastructure/Auth/` samler det som trengs for tokenhåndtering og oppslag av permissions. Hos oss betyr det i praksis JWT-generering, lesing av claims og logikken som finner hvilke permissions en bruker får gjennom rollene sine.

## Struktur

```text
Infrastructure/Auth/
├── IJwtService.cs        <- Interface for JWT-operasjoner
├── JwtService.cs         <- Implementasjon for token-generering og validering
├── JwtSettings.cs        <- Konfigurasjonsobjekt (Secret, Issuer, Audience, osv.)
├── IPermissionService.cs <- Interface for permission-oppslag
└── PermissionService.cs  <- Implementasjon
```

## Hvordan vi bruker denne mappen

Tanken er å samle auth-relaterte tekniske detaljer på ett sted, i stedet for å spre JWT- og claim-logikk rundt i flere deler av prosjektet. Det gjør flyten enklere å følge når man jobber med innlogging, refresh tokens eller autorisasjon.

`JwtService` brukes til å opprette access tokens og lese claims fra utløpte tokens ved refresh. `PermissionService` brukes til å finne hvilke permissions som følger med brukerens roller.

## Retningslinjer

- Injiser `IJwtService`, ikke `JwtService` direkte.
- Secrets og andre innstillinger skal komme fra `JwtSettings`, ikke hardkodes i kode.
- Permissions legges inn som claims i JWT slik at API-et slipper å gjøre oppslag på nytt for hver request.

Poenget er ikke bare å være "ren" i arkitekturen, men å ha ett tydelig sted for sikkerhetslogikken som resten av backend bygger på.
