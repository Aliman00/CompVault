# Infrastructure

Alt som snakker med omverdenen: database, filsystem, e-post, bakgrunnsjobber.

**Undermapper:**
- `Data/` — AppDbContext, migrasjoner, repository-implementasjoner, EF-konfigurasjoner
- `Auth/` — JWT-generering og token-håndtering
- `Extensions/` — ServiceCollection-extensions som registrerer alt i DI
- `BackgroundJobs/` — nattlige jobber (kompetansestatus, e-postvarsler)

Ingen kode utenfor Infrastructure skal kjenne til EF Core direkte.
