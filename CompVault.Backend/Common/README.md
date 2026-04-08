# Common

`Common/` brukes til kode som flere deler av backend trenger, men som ikke hører hjemme i én bestemt feature. Det er altså ikke stedet for forretningslogikk, men for felles byggeklosser som går igjen på tvers av systemet.

## Struktur

```text
Common/
├── Controller/           <- Base-klasser og felles funksjonalitet for API-lag
├── Middleware/           <- HTTP-middleware (f.eks. exception handling)
├── Authorization/        <- Autorisasjons-handlers og policyer
├── Responses/            <- Response-buildere og formattering
└── <Kategori>/           <- ny undermappe ved behov
```

## Hvordan vi bruker denne mappen

Tanken med `Common/` er å samle ting som faktisk er felles, i stedet for å kopiere samme type kode inn i flere features. Hvis noe bare gir mening i én modul, hører det som regel ikke hjemme her.

Et viktig skille er at vanlige controllere ikke ligger i `Common/`. `AuthController`, `UsersController` og lignende ligger fortsatt under `Features/*/Controllers/`, mens `Common/Controller/` bare er for base-klasser og delt API-funksjonalitet.

## Retningslinjer

- Ikke legg feature-spesifikk kode her.
- Ikke legg domene-typer som DTO-er, enums eller konstanter her; de hører hjemme i `CompVault.Shared`.
- Kode i `Common/` bør kunne brukes av flere features uten å dra med seg unødvendige avhengigheter.
- Nye filer eller mapper bør bare legges til her hvis de faktisk dekker et behov som går på tvers av flere features.

Målet er egentlig bare å holde denne mappen ryddig. Hvis alt mulig "som ikke passer noe sted" havner i `Common/`, blir den fort en oppsamlingsplass i stedet for noe som faktisk er nyttig.
