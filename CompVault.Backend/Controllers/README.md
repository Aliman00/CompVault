# Controllers

> Tynne HTTP-lag som mottar requests, delegerer til services og returnerer responses. Ingen forretningslogikk hører hjemme her.

## Innhold

| Fil | Ansvar |
|---|---|
| `BaseController.cs` | Abstrakt base med felles hjelpemetoder for alle controllers |
| `AuthController.cs` | Endepunkter for autentisering og OTP-flyt |
| `UsersController.cs` | Endepunkter for brukeradministrasjon |

## Regler

- Kall kun services via interface (f.eks. `IAuthService`) — aldri direkte implementasjon
- Returner alltid `ActionResult<T>` eller `IActionResult`
- Bruk `[ApiController]` og `[Route("api/[controller]")]` paa alle klasser
- Valider input via DataAnnotations — ikke manuelt i controlleren

## Ny controller? Gjør slik

1. Opprett `<Feature>Controller.cs` og arv fra `BaseController`
2. Injiser riktig service-interface via konstruktøren
3. Definer kun ruting og serialisering — all logikk ligger i servicen
