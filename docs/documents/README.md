# Dokumentmodulen — Moduldokumentasjon

Dokumentmodulen er den største og mest komplekse modulen i CompVault. Den håndterer alt fra dokumenttyper og kategorier til filopplasting, versjonering, signering og målgruppestyring. Her går vi gjennom hvordan det hele henger sammen.

## 1. Problemstilling og behov

Utgangspunktet for modulen var:
> Hvordan kan en bedrift organisere og distribuere dokumenter til de riktige ansatte, med støtte for versjonering, signering og fleksibel målgruppestyring?

Konkrete krav til løsningen:
- Kunne definere dokumenttyper (f.eks. "HMS Dokumenter", "Stillingsinstrukser") med egne kategorier, filtype-begrensninger og målgrupperegler.
- Støtte filopplasting med MIME-validering og størrelsesbegrensning per dokumenttype.
- Versjonere dokumenter — nye opplastinger arkiverer gamle filer, øker versjonsnummeret og sletter eksisterende signaturer.
- Signere dokumenter — brukere som er i målgruppen kan signere siste versjon. Etter opplasting av ny versjon må alle signere på nytt.
- Definere målgrupper per dokument via `DocumentTargetMode`:
  - `None`: alle kan se
  - `Department`: rettet mot spesifikke avdelinger
  - `JobTitle`: rettet mot spesifikke stillingstitler
- Håndtere tilgang via permissions: `documents:read`, `documents:write`, `documents:delete`, `documents:sign`, `documents:all:departments`, `documents:read:sub`.
- Støtte soft delete på dokumenter, dokumenttyper og kategorier.

## 2. Teknisk design

### Datamodell

Modulen har 7 entiteter: `Document`, `DocumentType`, `DocumentTypeCategory`, `DocumentVersion`, `DocumentSignature`, `DocumentDepartment` og `DocumentJobTitle`. Den fullstendige datamodellen er dokumentert i `documents-er-diagram.pdf`.

**DocumentType:**

| Felt | Type | Hva det er |
|------|------|------------|
| `Id` | Guid | PK |
| `Name` | varchar(100) | Visningsnavn, f.eks. "HMS Dokumenter" |
| `Slug` | varchar(50) | URL-vennlig slug, unik i systemet |
| `Description` | varchar(500) | Valgfri beskrivelse |
| `TargetMode` | varchar(20) | `None`, `Department` eller `JobTitle` |
| `StorageFolder` | varchar(100) | Undermappe i fillagring, settes til slug |
| `AllowedMimeTypes` | text[] | Tillatte MIME-typer for opplasting |
| `MaxFileSizeBytes` | bigint | Maks filstørrelse, default 20 MB |
| `IsActive` | bool | Soft delete-flag |
| `CreatedById` | Guid? | Hvem som opprettet (OnDelete: SetNull) |

**Document:**

| Felt | Type | Hva det er |
|------|------|------------|
| `Id` | Guid | PK |
| `DocumentTypeId` | Guid | FK til DocumentType (OnDelete: Restrict) |
| `DocumentTypeCategoryId` | Guid? | FK til DocumentTypeCategory |
| `Title` | varchar(200) | Dokumenttittel |
| `Description` | varchar(2000) | Valgfri beskrivelse |
| `ExternalUrl` | varchar(500) | Ekstern lenke |
| `RequiresSignature` | bool | Krever signering? default true |
| `Version` | int | Starter på 1, økes ved opplasting |
| `FileName` | varchar(255) | Originalt filnavn |
| `FilePath` | varchar(500) | Sti til fil på disk |
| `FileSize` | bigint? | Størrelse i bytes |
| `MimeType` | varchar(100) | MIME-type |
| `Checksum` | varchar(64) | SHA256-sjekksum for integritet |
| `UploadedBy` | Guid | FK til ApplicationUser (OnDelete: Restrict) |
| `IsActive` | bool | Soft delete-flag |

### Målgruppe-targeting

Hver dokumenttype har en `TargetMode` som bestemmer hvordan dokumenter rettes mot brukere. Dette styres via to many-to-many koblingstabeller:

- **DocumentDepartment** — kobler dokument til avdelinger. Brukes når `TargetMode = Department`.
- **DocumentJobTitle** — kobler dokument til stillingstitler. Brukes når `TargetMode = JobTitle`.

Ved `TargetMode = None` ignoreres begge lister og dokumentet er synlig for alle med lesetilgang.

`DocumentTargetingService` styrer all målgruppe-logikk. `CanUserAccessDocument` sjekker om en brukers avdeling eller stillingstittel matcher dokumentets lister. `CheckAccessAsync` legger til bypass-logikk for administratorer. `ValidateTarget` sikrer at target-listene er konsistente med dokumenttypens TargetMode — f.eks. kan du ikke sette avdelinger på en `TargetMode.JobTitle`.

### Filversjonering

Når en ny fil lastes opp via `POST /api/documents/{slug}/{id}/upload`:

1. Filen skrives til en midlertidig plassering på disk
2. Sjekksum (SHA256) beregnes — hvis den er identisk med forrige versjon, avvises opplastingen
3. Gammel fil flyttes fra `/active/{id}/` til `/archived/{id}/` med tidsstempel i filnavnet
4. En `DocumentVersion`-record opprettes med gammel metadata
5. Alle eksisterende `DocumentSignature`-rader slettes — brukere må signere på nytt
6. Dokumentets metadata oppdateres: versjon++, nye filinfo-felter
7. Alt persisteres i ett `SaveChangesAsync` — atomisk

Hvis DB-godkjennelsen feiler, ryddes temp-filen opp. Hvis den lykkes, flyttes temp-filen til endelig plassering.

### Signering

`POST /api/documents/{slug}/{id}/sign` krever permission `documents:sign`. `DocumentSignatureService.SignAsync`:

1. Sjekker at dokumentet krever signering
2. Sjekker at brukeren er i målgruppen (via `DocumentTargetingService.CheckAccessAsync`)
3. Sjekker at brukeren ikke allerede har signert denne versjonen
4. Oppretter en `DocumentSignature`-rad med `SignatureVersion = document.Version`

Signaturstatus kan hentes via `GET /{id}/signatures`, som returnerer en liste over alle målgruppe-brukere med `HasSigned`-flagg. Signaturene sorteres med usignerte først.

### Arkitektur

Modulen har 6 services som hver har et tydelig avgrenset ansvar. `DocumentsController` bruker `IDocumentService`, `IDocumentVersioningService` og `IDocumentSignatureService`. `DocumentTypesController` bruker `IDocumentTypeService`. Under disse ligger 4 repositories og `IDocumentFileService` som wrapper fillagring.

Samspillet er vist i `documents-arkitektur.png` eller mer detaljert i `documents-arkitektur-avansert.png`.

**Komponentoversikt:**

| Komponent | Type | Ansvar |
|-----------|------|--------|
| `DocumentsController` | Controller | 10 endepunkter: CRUD for dokumenter, opplast versjon, signer, last ned, signatur-status, brukerens dokumenter |
| `DocumentTypesController` | Controller | 10 endepunkter: CRUD for dokumenttyper og kategorier |
| `DocumentService` | Service | CRUD for dokumenter, målgruppe-filtrering, batch signatur-statistikk via `MapToListDtos` |
| `DocumentTypeService` | Service | CRUD for dokumenttyper og kategorier, slug-generering og unikhet |
| `DocumentSignatureService` | Service | Signering + signaturstatus (målgruppe-brukere × signaturer) |
| `DocumentVersioningService` | Service | Filopplasting med versjonering: arkivering, sjekksum, signatursletting, temp/arkiv flyt |
| `DocumentTargetingService` | Service | All målgruppe-logikk: tilgang, validering, avdelingshierarki |
| `DocumentFileService` | Service | Tynn wrapper rundt `IFileStorageService` + MIME/str validering |
| `DocumentMapper` | Statisk klasse | 7 mapping-metoder: Document↔DTO, DocumentType↔DTO, list-DTO med signaturstatistikk |
| `IDocumentRepository` | Repository | 9 metoder inkl. GetDocumentsForUser, GetDocumentTypesForUser, SoftDelete |
| `IDocumentTypeRepository` | Repository | 5 metoder: slug-oppslag, categories, SlugExists |
| `IDocumentSignatureRepository` | Repository | 5 metoder: HasUserSignedVersion, GetByDocumentIds, Remove |
| `IDocumentTypeCategoryRepository` | Repository | 2 metoder: GetByDocumentTypeId, SlugExists |

### Designvalg

| Valg | Hvorfor |
|------|--------|
| **TargetMode på dokumenttypen, ikke per dokument** | Konsistent oppførsel for alle dokumenter av samme type. Ingen overraskelser. |
| **Separate join-tabeller for targeting** | `DocumentDepartment` og `DocumentJobTitle` gir ren many-to-many uten å forurense Document-entiteten. EF Core håndterer dette via `HasQueryFilter` for å skjule slettede dokumenter. |
| **Atomisk versjonering uten transaksjon** | Skriving til disk kan ikke rulles tilbake — derfor gjøres DB-commit først, deretter filoperasjoner med logging av feil. |
| **Signaturer slettes ved ny versjon** | Tvinger alle til å signere siste versjon. Ingen "jeg signerte v1 for 6 mnd siden"-problematikk. |
| **Sjekksum-basert duplikatdeteksjon** | SHA256-sammenligning stopper re-upload av identiske filer — sparer lagring og unngår unødvendige versjonshopp. |
| **Batch signatur-statistikk** | `MapToListDtos` henter alle signaturer i én spørring og teller per dokument — unngår N+1. |
| **Slug-generering via SlugUtility** | URL-vennlige slugs fra navn, med unikhetssjekk. Forenkler frontend-ruting. |
| **OnDelete: Restrict på DocumentType → Document** | Forhindrer sletting av dokumenttyper som har dokumenter. Må ryddes manuelt. |
| **Ingen transaksjoner i Documents-modulen** | `DocumentService.CreateAsync` håndterer opprydding manuelt ved DbUpdateException. Enklere enn UnitOfWork når filoperasjoner er involvert. |

## 3. Implementasjon

`DocumentService` er den sentrale tjenesten. `GetAllAsync` henter dokumenter for en dokumenttype og filtrerer på målgruppe hvis brukeren ikke har bypass (`DocumentsWrite`). Den henter alle signaturer i batch og bruker `DocumentMapper.MapToListDtos` for å beregne signaturstatistikk per dokument i minnet. `GetByIdAsync` sjekker tilgang og om brukeren har signert gjeldende versjon. `GetDocumentsForUserAsync` støtter "mine dokumenter"-visningen med paginering og filtrering på signaturstatus (`Signed`/`Unsigned`/`All`).

`CreateAsync` er den mest omfattende metoden — den validerer dokumenttype, kategori, target-lister, avdelinger (eksistens + tilgang), stillingstitler, MIME-type og filstørrelse før dokumentet opprettes. Hvis noe feiler under DB-lagring, ryddes den opplastede filen opp. `UpdateAsync` støtter partial update med `ClearExternalUrl` og `ClearDocumentTypeCategoryId`, og inkluderer `ApplyTargetingUpdate` som rydder opp i målgruppe-listene basert på dokumenttypens TargetMode — ved endring av TargetMode tømmes den andre listen automatisk.

`DocumentTypeService` administrerer dokumenttyper og kategorier i samme klasse (via `IDocumentTypeCategoryRepository`). Slug-generering skjer automatisk fra navn med `SlugUtility.GenerateSlug`, og unikhet sjekkes før lagring. Ved oppdatering av kategorinavn regenereres slug og sjekkes for konflikter.

`DocumentVersioningService.UploadVersionAsync` er en nøye orkestrert 4-fase-prosess: skriv temp-fil → forbered DB-endringer → commit DB → flytt filer på disk. Hver fase har feilhåndtering med opprydding. `GetDownloadAsync` returnerer file metadata, og `OpenFileStreamAsync` åpner filen for streaming — dette gjøres i controlleren slik at ASP.NET Core kan håndtere disposal.

`DocumentSignatureService.SignAsync` sjekker tre forhold før signatur opprettes: dokumentet må kreve signatur, brukeren må være i målgruppen, og brukeren må ikke allerede ha signert. `GetSignatureStatusAsync` slår sammen målgruppe-brukere og signaturer via `GetUsersByTargetAsync` og mapper til `UserSignatureStatusDto`.

`DocumentTargetingService` orkestrerer tilgangssjekker, validering av target-lister og avdelingshierarki. `CheckAccessAsync` returnerer `Forbidden` hvis brukeren ikke er i målgruppen. `CanUserAccessDocument` er en ren in-memory-sjekk for liste-filtrering. `CheckDepartmentPermissionAsync` sjekker at brukeren har tilgang til både avdelinger som legges til og de som fjernes fra et dokument.

`DocumentFileService` er en tynn wrapper rundt `IFileStorageService` (LocalFileStorageService) og legger til MIME- og størrelsesvalidering. `ComputeChecksumAsync` bruker SHA256 for filintegritet.

## 4. Utfordringer og beslutninger

### Filoperasjoner og database-transaksjoner

Dette var den største arkitektoniske utfordringen i modulen. Du kan ikke wrappe filoperasjoner og DB-operasjoner i én transaksjon — filsystemet støtter ikke rollback. `DocumentVersioningService.UploadVersionAsync` løser dette med en "DB-first, files-second"-strategi: alle DB-endringer samles i ett `SaveChangesAsync`. Hvis det feiler, ryddes temp-filen opp og ingenting er lagret. Hvis DB-godkjennelsen lykkes, flyttes filene på disk med feillogging — filene ligger der de er hvis flytting feiler, men databasen er konsistent.

### Målgruppe-kompleksitet

Tre TargetModes, to separate join-tabeller, avdelingshierarki, og permission-bypass — det er mye logikk. Vi valgte å samle alt i `IDocumentTargetingService` i stedet for å spre det utover. `ValidateTarget` sørger for at frontend ikke kan sende inn avdelinger på en `TargetMode.JobTitle`. `ApplyTargetingUpdate` i `DocumentService` rydder automatisk opp i den andre listen når TargetMode endres — uten dette kunne et dokument ha foreldreløse `DocumentJobTitle`-rader etter at typen ble endret til `Department`.

### Signaturer ved versjonsbytte

Da vi implementerte versjonering, innså vi at signaturer på gamle versjoner blir meningsløse. Løsningen ble å slette alle signaturer ved ny versjon — `DocumentVersioningService` kaller `signatureRepository.Remove()` på hver tracked signatur. Dette tvinger brukere til å signere på nytt og sikrer at signaturstatus alltid reflekterer gjeldende versjon.


### Ekstern URL vs. filvedlegg

Et dokument kan ha enten en ekstern URL, en opplastet fil, eller ingen av delene. Vi valgte å ikke tvinge frem "minst én av dem" — et tomt dokument kan være en placeholder eller en ren tekst-beskrivelse. `HasFile`-flagget i `DocumentDto` lar frontend vite om nedlastingsknappen skal vises.

## 5. Relaterte moduler

| Modul | Relasjon |
|-------|----------|
| **Users** | `Document.UploadedBy` → `ApplicationUser`; `DocumentSignature.UserId` → `ApplicationUser`; `GetUsersByTargetAsync` brukes av `DocumentSignatureService` |
| **Departments** | `DocumentDepartment.DepartmentId` → `Department`; avdelingshierarki brukes i `DocumentTargetingService` |
| **JobTitles** | `DocumentJobTitle.JobTitleId` → `JobTitle`; `IsLeader` brukes for å finne potensielle ledere |
| **Auth** | Permissions `documents:read/write/delete/sign`, `documents:all:departments`, `documents:read:sub`, `document_types:read/write/delete` |
| **Audit** | `IAuditContext.SetActionOverride("document.upload_version")` i `DocumentVersioningService` |
| **FileStorage** | `IFileStorageService` (LocalFileStorageService) via `DocumentFileService` |
