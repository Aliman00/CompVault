# Controllers

Tynne HTTP-lag. En controller skal aldri inneholde forretningslogikk.

**Regler:**
- Kall kun services via interface (f.eks. `IAuthService`)
- Returner alltid `ActionResult<T>` eller `IActionResult`
- Bruk `[ApiController]` og `[Route("api/[controller]")]` på alle klasser
- Valider input via DataAnnotations eller FluentValidation — ikke manuelt i controlleren