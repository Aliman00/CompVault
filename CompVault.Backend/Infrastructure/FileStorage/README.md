# Infrastructure/FileStorage

Her ligger koden som håndterer lagring av filer. I CompVault brukes det hovedsakelig til dokumenter som lastes opp.

## Abstraksjon

Vi har skilt fillagringen bak et interface (`IFileStorageService`) så det er mulig å bytte til en annen løsning — for eksempel S3 eller Azure Blob — senere uten å måtte endre kode i DocumentService eller andre steder.

## Struktur

```text
Infrastructure/FileStorage/
├── Configuration/
│   └── FileStorageSettings.cs  <- Konfigurasjon for rotmappe (f.eks. "storage")
├── IFileStorageService.cs      <- Interface: Save, Delete, Move, OpenRead, ComputeChecksum
└── LocalFileStorageService.cs  <- Implementasjon med lokal disk + path-traversal-beskyttelse
```

## Lokal lagring

`LocalFileStorageService` lagrer filer på disk under en rotmappe som konfigureres i `appsettings.json`. Den bruker relative stier, og validerer at filoperasjoner holder seg innenfor rotmappen for å unngå at noen går ut av det de har tilgang til.

## Retningslinjer

- Injiser `IFileStorageService`, ikke `LocalFileStorageService` direkte.
- Rotmappen settes i `appsettings.json` via `FileStorageSettings`, ikke hardkodet.
- Ved bytte til sky-lagring oppretter du `CloudFileStorageService`, implementerer `IFileStorageService`, og bytter registrering i DI.
