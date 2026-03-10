# Common

Delt kode uten forretningsverdi — brukes på tvers av alle features i Backend.

**Innhold:**
- `Result.cs` — OperationResult<T>, bruk denne som returtype fra alle services
- `Errors/` — AppError og ErrorCode enum
- `Middleware/` — GlobalExceptionMiddleware
- `Pagination/` — PagedResult<T> for liste-endepunkter (legges til ved behov)

> **Merk:** Permissions-konstanter (f.eks. `"users:read"`) ligger i
> `CompVault.Shared/Constants/Permissions.cs` — ikke her — fordi
> Frontend også trenger dem for å vise/skjule UI-elementer.

Legg IKKE feature-spesifikk kode her.
