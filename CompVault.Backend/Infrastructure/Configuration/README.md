# Infrastructure/Configuration

Her samles konfigurasjoner og oppstartshjelpere som blir brukt før appen kjører. Dette er ikke forretningslogikk, men logikk som finner ut hvordan appen skal kobles sammen.

## Struktur

```text
Infrastructure/Configuration/
├── ConfigurationLoader.cs    <- Laster .env-filen med miljøvariabler før appen starter
├── ConfigurationValidator.cs <- Sjekker at påkrevde miljøvariabler er satt ved oppstart
└── CorsSettings.cs          <- CORS-innstillinger hentet fra appsettings.json
```

## Konfigurasjonsflyt

1. **ConfigurationLoader** finner `.env`-filen og laster variablene inn. Kallet gjøres fra `Program.cs` aller først, slik at `IConfiguration` plukker opp verdiene.
2. **ConfigurationValidator** kjøres rett etterpå og sjekker at alle required innstillinger er satt — database, JWT, e-post og CORS. Mangler noe, starter appen ikke i det hele tatt.
3. **CorsSettings** brukes til å binde frontend-URLene fra `appsettings.json`, separert med komma.

## Retningslinjer

- Ingen miljøvariabler hardkodes i kode — det er `.env` eller `appsettings.json` som skal brukes.
- Legg til nye påkrevde innstillinger ved å utvide `ConfigurationValidator` slik at appen feiler ved manglende konfigurasjon.
- CorsSettings har en `SectionName` som må matche navnet på seksjonen i `appsettings.json`.
