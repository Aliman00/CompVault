# Infrastructure

> Alt som kommuniserer med omverdenen: database, e-post og autentisering. Ingen kode utenfor denne mappen skal ha direkte kjennskap til EF Core.

## Struktur

```
Infrastructure/
  Data/           <- AppDbContext, IUnitOfWork, UnitOfWork, EF-konfigurasjoner og domenerepos
  Auth/           <- IJwtService, JwtService, JwtSettings
  Email/          <- IEmailService, EmailService, konfig, maler og modeller
  Repositories/   <- IRepository<T> og BaseRepository<T> (generisk base)
  Extensions/     <- ServiceCollectionExtensions, WebApplicationBuilderExtensions
```

## Regler

- Ingen kode utenfor `Infrastructure` skal importere EF Core-navnerom direkte
- `AppDbContext` brukes kun fra `Infrastructure` og eventuelt entrypoint 
(`Program.cs`) for f.eks. health checks
- Repositories kaller aldri `SaveChangesAsync()` — det eies av service-laget via `IUnitOfWork`
