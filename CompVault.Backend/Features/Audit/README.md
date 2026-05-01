# Audit

`Features/Audit/` inneholder det som trengs for å hente ut og vise revisjonsloggen. Selve loggingen skjer automatisk i `AuditSaveChangesInterceptor` — denne featuren er bare lesesiden av det.

## Hva er revisjonsloggen?

All endring i CompVault som har med data å gjøre, havner i revisjonsloggen. Opprettelser, endringer, slettinger, tilbakekallinger, signeringer — alt blir liggende med hvem, hva, når og hvorfor. Dette er påkrevd for å tilfredsstille for eks. Arbeidstilsynets dokumentasjonskrav der ting systematisk blir logget. 

## Struktur

```text
Features/Audit/
├── AuditLogMapper.cs         <- Mapper fra entitet til DTO
├── Controllers/
│   └── AuditController.cs    <- GET /api/audit-log med filtrering og paginering
└── Services/
    ├── IAuditContext.cs      <- Lar services sette kontekst før logging
    ├── AuditContext.cs       <- Implementasjon (scoped per request)
    ├── IAuditLogService.cs   <- Interface for spørringer mot loggen
    └── AuditLogService.cs    <- Implementasjon
```

## Hvordan loggingen fungerer

Loggingen kjører stort sett på autopilot. Når en service kaller `SaveChangesAsync()`, fanger `AuditSaveChangesInterceptor` opp hva som har skjedd via EF Core sin ChangeTracker og lager revisjonsoppføringer automatisk. Dette skjer i samme transaksjon som selve databaseendringen, så alt lagres atomisk.

Unntaksvis, når den automatiske loggingen ikke er presis nok, kan en service gi ekstra kontekst via `IAuditContext`. Det brukes for eksempel når en kompetanse tilbakekalles — da overstyres action fra `competency.update` til `competency.revoke` og en årsak legges ved.

Bakgrunnsjobber som bruker `ExecuteUpdateAsync` går utenom ChangeTracker og må logge manuelt. Dette gjelder foreløpig bare `CompetencyStatusJob`.

For en mer detaljert gjennomgang av hvordan interceptoren fungerer, se `Infrastructure/Data/Interceptors/README.md`.

## API-et

`GET /api/audit-log` — krever `audit:read`. Støtter filtrering på:

- `action` — hvilken type handling (f.eks. `competency.revoke`)
- `entityType` — hvilken entitetstype (f.eks. `Competency`)
- `entityId` — en spesifikk entitet
- `userId` — hvem som utførte handlingen
- `from` / `to` — tidsrom
- `page` / `pageSize` — paginering

## Hva logges og hva logges ikke

Alle vesentlige endringer logges: kompetanser, dokumenter, signaturer, avdelinger, brukere, roller, stillingstitler og utstyr.

Det som **ikke** logges er ting som bare er teknisk støy — engangskoder, refresh tokens, interne join-tabeller og selve revisjonsloggen. Disse hoppes over av interceptoren.
