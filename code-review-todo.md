# Code review – småting å rydde opp i

## Advarsler

### `RefreshTokenRequest` har et `AccessToken`-felt som aldri brukes
**Fil:** `CompVault.Shared/DTOs/Auth/RefreshTokenRequest.cs`

`RefreshTokenRequest` inneholder et `[Required] AccessToken`-felt, men `AuthService.RefreshTokenAsync` leser kun `request.RefreshToken` — access token brukes aldri. Dette forvirrer API-konsumenter og tvinger klienten til å sende begge feltene unødvendig.

**Fiks:** Fjern `AccessToken` fra DTO-en. Standard refresh-flyt trenger kun refresh token.

---

### `OtpOptions` – `MinResponseTimeRequestOtpMs` og `MinResponseTimeVerifyOtpMs` bruker `set`, ikke `init`
**Fil:** `CompVault.Backend/Features/Auth/Configuration/OtpOptions.cs`

`MaxFailedAttempts` og `ExpirationMinutes` bruker `init` (immutable etter binding), men de to timing-egenskapene bruker `set`. Sannsynligvis en tilfeldighet.

**Fiks:** Bytt `set` til `init` på begge for konsistens.

---

### `EmailSettings` er ikke `sealed`
**Fil:** `CompVault.Backend/Infrastructure/Email/Config/EmailSettings.cs`

`JwtSettings` og `OtpOptions` er begge `sealed`, men `EmailSettings` er det ikke.

**Fiks:** Legg til `sealed` på klassen.

---

## Skrivefeil i kommentarer

| Fil | Feil | Riktig |
|-----|------|--------|
| `Domain/Entities/Auth/OtpCode.cs` | `LastAttemptAt` har samme kommentar som `FailedAttempts` | `/// Tidspunktet for siste forsøk` |
| `Domain/Entities/Identity/ApplicationUser.cs` | `Brukren som opprettet brukeren` | `Brukeren som opprettet brukeren` |
| `Domain/Entities/Identity/RolePermission.cs` | `Brukeren som ga brukeren tillattelsen` | `Brukeren som tildelte tillatelsen` |
| `Features/Auth/Services/AuthService.cs` | `testest grunding` (to steder) | `testes grundig` |
| `Infrastructure/Extensions/WebApplicationBuilderExtensions.cs` | `objeektet` | `objektet` |
| `Infrastructure/Data/UnitOfWork.cs` | `transkasjonen` | `transaksjonen` |
| `Shared/Result/ErrorCode.cs` | `Unventede` | `Uventede` |
| `Shared/Result/Result.cs` | Dobbelt `///` foran `</summary>` | Fjern det ekstra `///` |
