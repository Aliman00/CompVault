# Infrastructure/Extensions

Extension-metoder på `IServiceCollection` og `IApplicationBuilder` som samler all DI-registrering og middleware-oppsett. Kalles fra `Program.cs` for å holde oppstartskoden kortfattet.

## Filer

- `ServiceCollectionExtensions.cs` — registrerer databaser, autentisering, repositories og services
- `ApplicationBuilderExtensions.cs` — konfigurerer middleware-pipeline (f.eks. global exception handling)

---

## Ny feature? Du må gjøre to ting her

### 1. Registrer repository i `AddRepositories()`

```csharp
public static IServiceCollection AddRepositories(this IServiceCollection services)
{
    services.AddScoped<IUserRepository, UserRepository>();
    services.AddScoped<IMinFeatureRepository, MinFeatureRepository>(); // ← legg til
    return services;
}
```

### 2. Registrer service i `AddApplicationServices()`

```csharp
public static IServiceCollection AddApplicationServices(this IServiceCollection services)
{
    services.AddScoped<IAuthService, AuthService>();
    services.AddScoped<IUserService, UserService>();
    services.AddScoped<IMinFeatureService, MinFeatureService>(); // ← legg til
    return services;
}
```

---

## Levetid

Alle repositories og services registreres med `AddScoped` — én instans per HTTP-request. Ikke bruk `AddSingleton` for noe som holder tilstand eller bruker `AppDbContext`.
