# CompVault.Shared

> Delt kontraktsbibliotek referert av **baade** `CompVault.Backend` og `CompVault.Frontend`. Gir én enkelt kilde til sannhet for alle typer som flyter mellom lagene.

## Struktur

```
CompVault.Shared/
  DTOs/
    Auth/        <- RequestOtpRequest, VerifyOtpRequest, LoginResponse, RefreshTokenRequest
    Users/       <- UserDto, CreateUserRequest, UpdateUserRequest
    <Feature>/   <- opprettes per fase
  Enums/
    EmploymentType.cs
    OtpDeliveryMethod.cs
  Constants/
    Permissions.cs
  Result/
    Result.cs          <- Result<T> — returtype fra alle services
    AppError.cs        <- feiltype med melding og kode
    ErrorCode.cs       <- enum med alle feilkoder
    ProblemDetail.cs   <- serialiserbar feilrespons til Frontend
```

## Hva hører hjemme her

| Type | Eksempel | Begrunnelse |
|---|---|---|
| DTOs og response-modeller | `UserDto`, `LoginResponse` | Frontend deserialiserer API-respons til disse |
| Request-modeller | `CreateUserRequest`, `RequestOtpRequest` | En felles definisjon holder Frontend og Backend synkronisert |
| Enums | `EmploymentType`, `OtpDeliveryMethod` | Brukes av begge lag — dropdowns i Frontend, validering i Backend |
| Konstanter | `Permissions.cs` | Backend: autorisasjonspolicyer. Frontend: vise/skjule UI-elementer |
| Result-typer | `Result<T>`, `AppError`, `ErrorCode` | Backend returnerer disse; Frontend bruker dem til feilhaandtering |

## Hva hører IKKE hjemme her

| Type | Begrunnelse |
|---|---|
| Entiteter (`ApplicationUser` osv.) | Har EF Core-avhengigheter og navigasjonsegenskaper Frontend ikke trenger |
| Services og Repositories | Backend-spesifikk logikk |
| Infrastructure (JWT, DbContext) | Hører ikke hjemme i et delt bibliotek |

## Ny fase? Gjør slik

1. Opprett `DTOs/<FeatureName>/` med request- og response-klasser
2. Opprett `Enums/<NyEnum>.cs` for eventuelle nye enums
3. Legg til nye konstanter nederst i `Constants/Permissions.cs`

Namespace-konvensjon:

```csharp
namespace CompVault.Shared.DTOs.<FeatureName>;
namespace CompVault.Shared.Enums;
namespace CompVault.Shared.Constants;
namespace CompVault.Shared.Result;
```
