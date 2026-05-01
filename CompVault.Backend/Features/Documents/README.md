# Documents

Dette er den største featuren i CompVault og håndterer alt som har med dokumenter å gjøre. Enkelt sagt: vi har dokumenttyper, dokumenter som hører til en type, filer som kan lastes opp til dokumentene, versjonering av filene, og signering.

## Hvorfor så mange services?

Dokumenter har ganske mye logikk rundt seg, så vi har delt det opp i seks services i stedet for å stappe alt i én. Hver service har ett tydelig ansvar:

- **DocumentService** — det grunnleggende: opprett, hent, oppdater og slett dokumenter
- **DocumentTypeService** — administrere dokumenttyper eller kategorier om man vil
- **DocumentFileService** — lagre og validere filer på disk, regne ut sjekksum
- **DocumentVersioningService** — laste opp nye versjoner, arkivere gamle, og nedlasting
- **DocumentSignatureService** — signering, sjekke signaturstatus, og hente "mine signerte/ventende"
- **DocumentTargetingService** — håndtere målgrupper (hvem skal se/signere dokumentet?)

## Hvordan målgrupper fungerer

`TargetMode` settes på dokumenttypen og styrer hvem dokumenter av den typen er rettet mot:

- **None** — alle kan se dokumentet
- **Department** — bare ansatte i valgte avdelinger kan se det
- **JobTitle** — bare ansatte med valgte stillingstitler kan se det

Når du oppretter et dokument, velger du en dokumenttype og dermed arver dokumentet typens `TargetMode`. Deretter setter du hvilke konkrete avdelinger eller stillingstitler dokumentet gjelder for. Om dokumentet skal kreve signering bestemmes også når dokumentet opprettes — det ligger på selve dokumentet, ikke på typen.

## Versjonering

Når noen laster opp en ny fil til et dokument, økes versjonsnummeret. Den gamle filen arkiveres i en egen mappe og den gamle versjonen lagres i `DocumentVersions`-tabellen. Signaturer på den gamle versjonen fjernes, så alle må signere på nytt — dette logges i revisjonsloggen.

## Struktur

```
Features/Documents/
├── DocumentMapper.cs              <- Mapper fra entiteter til DTOer
├── Controllers/
│   ├── DocumentsController.cs     <- CRUD + opplasting, nedlasting, signering
│   └── DocumentTypesController.cs <- Administrere dokumenttyper og kategorier
└── Services/
    ├── IDocumentService.cs / DocumentService.cs
    ├── IDocumentTypeService.cs / DocumentTypeService.cs
    ├── IDocumentFileService.cs / DocumentFileService.cs
    ├── IDocumentVersioningService.cs / DocumentVersioningService.cs
    ├── IDocumentSignatureService.cs / DocumentSignatureService.cs
    └── IDocumentTargetingService.cs / DocumentTargetingService.cs
```
