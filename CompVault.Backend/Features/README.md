# Features

`Features/` er stedet der det meste av den faktiske backend-logikken bor. Vi har valgt å organisere dette per fagområde i stedet for per teknisk type, fordi det gjør det lettere å finne sammenhengende kode når man jobber med én del av systemet.

## Struktur

Hver feature ligger i sin egen mappe under `Features/`:

```text
Features/
├── <Feature1>/          <- f.eks. Auth, Users, Departments
├── <Feature2>/
└── <Feature3>/
```

I praksis betyr det at alt som hører naturlig sammen, prøver vi å holde samlet. Da slipper man å hoppe rundt mellom mange mapper bare for å følge én flyt.

## Oppsett per feature

En vanlig feature ser typisk slik ut:

```text
Features/<FeatureName>/
├── Services/
│   ├── I<FeatureName>Service.cs
│   └── <FeatureName>Service.cs
└── Controllers/
    └── <FeatureName>Controller.cs
```

Noen features kan ha flere services hvis det faktisk trengs. Vi prøver ikke å tvinge alt inn i én klasse hvis det gjør koden vanskeligere å lese.

## Retningslinjer

- Services injecteres via interface, ikke direkte via implementasjon.
- Public metoder i service-laget bruker som hovedregel `Result<T>` fra `CompVault.Shared/Result/`.
- DTO-er og request-modeller legges i `CompVault.Shared/DTOs/<FeatureName>/`, ikke inne i feature-mappen.
- Asynkrone metoder bør ta `CancellationToken ct = default`.

Dette er ikke ment som regler bare for reglenes skyld. Poenget er å holde feature-lagene noenlunde like, slik at det er lettere å lese og vedlikeholde prosjektet over tid.

## Når du lager en ny feature

1. Opprett `Features/<FeatureName>/Services/I<FeatureName>Service.cs`.
2. Opprett `Features/<FeatureName>/Services/<FeatureName>Service.cs`.
3. Opprett `Features/<FeatureName>/Controllers/<FeatureName>Controller.cs`.
4. Registrer servicen i `Infrastructure/Extensions/ServiceCollectionExtensions.cs`.
5. Opprett tilhørende DTO-mappe i `CompVault.Shared/DTOs/<FeatureName>/`.

Hvis en feature senere trenger flere services, repositories eller annen struktur, er det helt greit å utvide. Mappen trenger ikke låses til et minimalt oppsett så lenge den fortsatt er ryddig.
