# CompVault.Shared

Delt kontraktsbibliotek referert av både `CompVault.Backend` og `CompVault.Frontend`. Gir én enkelt kilde til sannhet for alle typer som flyter mellom lagene.

## Struktur

```text
CompVault.Shared/
├── DTOs/
│   ├── Auth/              ← RequestOtpRequest, VerifyOtpRequest, TokenResponse
│   ├── Users/             ← UserDto, CreateUserRequest, UpdateUserRequest
│   ├── Documents/         ← DocumentDto, CreateDocumentRequest, DocumentTypeDto
│   ├── Competencies/      ← CompetencyDto, CreateCompetencyRequest
│   ├── Departments/       ← DepartmentDto, CreateDepartmentRequest
│   ├── Equipment/         ← EquipmentCategoryDto, EquipmentIssuanceDto
│   ├── JobTitles/         ← JobTitleDto, CreateJobTitleRequest
│   ├── Roles/             ← RoleDto, PermissionDto, AssignPermissionsRequest
│   ├── Audit/             ← AuditLogDto, AuditLogQueryParameters
│   └── Common/            ← Pagination (PagedQuery, PagedResult)
├── Enums/
│   ├── CompetencyStatus.cs
│   ├── DocumentSignatureFilter.cs
│   ├── DocumentSortField.cs
│   ├── DocumentTargetMode.cs
│   └── EmploymentType.cs
├── Constants/
│   ├── Permissions.cs           ← alle permission-strenger (users:read, documents:sign, osv.)
│   ├── ApiRoutes.cs             ← samling av alle endpoint-URLer
│   └── Validations/             ← MaxLength/MinLength + feilmeldinger per feature
├── Result/
│   ├── Result.cs        ← Result<T> og Result, returtype fra alle services
│   ├── AppError.cs      ← feil med melding + ErrorCode
│   ├── ErrorCode.cs     ← enum med alle feilkoder
│   └── ProblemDetail.cs ← serialiserbar feilrespons
```

## Hva hører hjemme her

- **DTOs og request-modeller** — Frontend deserialiserer API-respons til disse (`UserDto`, `DocumentDto`). Request-modeller (`CreateUserRequest`, `UpdateDocumentRequest`) holder Frontend og Backend synkronisert.
- **Enums** — Brukes av begge lag. Dropdowns i Frontend, validering i Backend. F.eks. `EmploymentType`, `DocumentTargetMode`, `CompetencyStatus`.
- **Konstanter** — `Permissions.cs` brukes av Backend for autorisasjon og av Frontend for å vise/skjule UI-elementer. `ApiRoutes.cs` samler alle endpoint-URLer. `*Validations`-klasser inneholder `MaxLength`/`MinLength` og feilmeldinger som deles.
- **Result-typer** — `Result<T>`, `AppError`, `ErrorCode`, `ProblemDetail`. Backend returnerer disse; Frontend bruker dem til feilhåndtering.

## Hva hører IKKE hjemme her

| Type | Begrunnelse |
|---|---|
| Entiteter (`ApplicationUser` osv.) | Har EF Core-avhengigheter og navigasjonsegenskaper Frontend ikke trenger |
| Services og Repositories | Backend-spesifikk logikk |
| Infrastructure (JWT, DbContext) | Hører ikke hjemme i et delt bibliotek |

## Ny fase? Gjør slik

1. Opprett `DTOs/<FeatureName>/` med request- og response-klasser.
2. Opprett `Enums/<NyEnum>.cs` for eventuelle nye enums.
3. Legg til nye konstanter i `Constants/` — bruk feature-spesifikke filer (`UserValidations`, `DocValidations`, osv.).

**Namespace-konvensjon:**

```csharp
namespace CompVault.Shared.DTOs.<FeatureName>;
namespace CompVault.Shared.Enums;
namespace CompVault.Shared.Constants;
namespace CompVault.Shared.Result;
```
