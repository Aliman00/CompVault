# CompVault.Shared

Delt kontraktsbibliotek referert av **både** `CompVault.Backend` og `CompVault.Frontend`.

Formålet er å ha én enkelt kilde til sannhet for alle typer som flyter mellom Frontend og Backend. Uten dette måtte Frontend enten duplisere klassene eller referere Backend-prosjektet direkte — og dra med seg EF Core, ASP.NET Identity og resten av Backend som unødvendig bagasje.

---

## Struktur

```
CompVault.Shared/
  DTOs/
    Auth/        ← LoginRequest, LoginResponse, RefreshTokenRequest
    Users/       ← UserDto, CreateUserRequest, UpdateUserRequest
    <Feature>/   ← opprettes per fase, f.eks. Competencies/, Documents/
  Enums/
    EmploymentType.cs
    <NyEnum>.cs  ← legg til her når nye faser introduserer enums
  Constants/
    Permissions.cs
```

---

## Hva hører hjemme her

| Type | Eksempel | Hvorfor |
|---|---|---|
| **DTOs / Response-modeller** | `UserDto`, `LoginResponse` | Frontend deserialiserer API-respons til disse typene |
| **Request-modeller** | `CreateUserRequest`, `LoginRequest` | Frontend bygger disse og sender til Backend — én felles definisjon holder begge synkronisert |
| **Enums** | `EmploymentType` | Frontend trenger dem for dropdowns og visning; Backend for validering og DB-lagring |
| **Konstanter** | `Permissions.cs` | Backend bruker dem i autorisasjonspolicyer; Frontend bruker dem til å vise/skjule UI-elementer |

## Hva som IKKE hører hjemme her

| Type | Hvorfor |
|---|---|
| **Entiteter** (`ApplicationUser` osv.) | Har EF Core-avhengigheter og navigasjonsegenskaper Frontend ikke trenger |
| **Services / Repositories** | Backend-spesifikk logikk |
| **Infrastructure** | JWT, DbContext, Hangfire — hører ikke hjemme i et delt bibliotek |

---

## Ny fase? Gjør slik

1. Opprett `DTOs/<FeatureName>/` med request- og response-klasser
2. Opprett `Enums/<NyEnum>.cs` for eventuelle nye enums i fasen
3. Legg til nye konstanter nederst i `Constants/Permissions.cs`

Namespace-konvensjonen er:
```csharp
namespace CompVault.Shared.DTOs.Auth;
namespace CompVault.Shared.DTOs.Users;
namespace CompVault.Shared.Enums;
namespace CompVault.Shared.Constants;
```
