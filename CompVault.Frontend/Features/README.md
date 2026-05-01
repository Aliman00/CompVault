# Features

Hoveddelen av appen. Én mappe per fagområde, akkurat som på backend. Tanken er den samme: alt som hører sammen ligger samlet.

## Struktur

Hver feature følger et mønster:

```text
Features/<FeatureName>/
├── Pages/           <- Routable .razor-sider (f.eks. brukerliste, detaljvisning)
├── Components/      <- Interne komponenter som bare denne featuren bruker
├── Services/        <- API-klienter og feature-spesifikke tjenester
├── Models/          <- View-modeller eller interne typer for featuren
└── Constants/       <- eventuell feature-spesifikke konstanter
```

Ikke alle features har alle undermapper. En liten feature trenger kanskje bare `Pages/`, mens en større som `Documents` har flere services og komponenter.

## Oppsett per feature

Når du lager en ny feature:

1. Opprett `Features/<FeatureName>/Pages/`
2. Opprett `Features/<FeatureName>/Services/` — her ligger API-klienten som kaller backend
3. Registrer API-klienten i `Extensions/ServiceCollectionExtensions.cs`
4. Opprett eventuelle interne komponenter i `Features/<FeatureName>/Components/`

## Viktigst av alt

En feature skal **ikkeImportere fra en annen features mappe**. Hvis `Documents` trenger noe fra `Users`, går den via `Common/` eller `CompVault.Shared`.

For full guide med eksempler, se `STRUCTURE.md` i rot-mappen.
