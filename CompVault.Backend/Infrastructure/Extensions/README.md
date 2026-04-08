# Infrastructure/Extensions

`Infrastructure/Extensions/` brukes for å samle oppstartskode som ellers fort gjør `Program.cs` unødvendig lang. Her ligger blant annet DI-registrering, middleware-oppsett og noen extension-metoder som brukes flere steder.

## Struktur

```text
Infrastructure/Extensions/
├── ServiceCollectionExtensions.cs      <- Registrerer database, autentisering, e-post, repositories og services
├── WebApplicationBuilderExtensions.cs  <- Konfigurerer middleware-pipeline
└── ClaimsPrincipalExtensions.cs        <- Extension-metoder for ClaimsPrincipal
```

## Hvordan vi bruker denne mappen

Målet her er egentlig ganske praktisk: å holde registrering og oppstart samlet på ett sted. Da blir `Program.cs` enklere å lese, og det er lettere å finne igjen hvor nye services, repositories eller middleware faktisk registreres.

Når vi legger til noe nytt i prosjektet, er dette ofte ett av de første stedene som må oppdateres. Derfor er det greit at strukturen er ganske forutsigbar.

## Når du registrerer en ny tjeneste

Repositories registreres i egne metoder som for eksempel `AddRepositories()`, mens services legges i `AddApplicationServices()`. Det gjør oppsettet litt mer oversiktlig enn om alt bare havner i én lang metode.

```csharp
public static IServiceCollection AddRepositories(this IServiceCollection services)
{
    services.AddScoped<IUserRepository, UserRepository>();
    services.AddScoped<IMinFeatureRepository, MinFeatureRepository>();
    return services;
}
```

```csharp
public static IServiceCollection AddApplicationServices(this IServiceCollection services)
{
    services.AddScoped<IAuthService, AuthService>();
    services.AddScoped<IUserService, UserService>();
    services.AddScoped<IMinFeatureService, MinFeatureService>();
    return services;
}
```

## Retningslinjer

- `AddScoped` er som regel riktig for repositories og services.
- `AddSingleton` bør ikke brukes for ting som holder tilstand eller er avhengige av `AppDbContext`.
- Del gjerne registreringen opp i metoder per ansvarsområde i stedet for å samle alt i én blokk.
