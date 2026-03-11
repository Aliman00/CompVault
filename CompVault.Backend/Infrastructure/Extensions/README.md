# Infrastructure/Extensions

> Samler all DI-registrering og middleware-konfigurasjon. Kalles fra `Program.cs` for aa holde oppstartskoden kortfattet.

## Innhold

| Fil | Ansvar |
|---|---|
| `ServiceCollectionExtensions.cs` | Registrerer database, autentisering, e-post, repositories og services |
| `WebApplicationBuilderExtensions.cs` | Konfigurerer middleware-pipeline (f.eks. global exception handling) |

## Ny feature? Gjør slik

### 1. Registrer repository i `AddRepositories()`

```csharp
public static IServiceCollection AddRepositories(this IServiceCollection services)
{
    services.AddScoped<IUserRepository, UserRepository>();
    services.AddScoped<IMinFeatureRepository, MinFeatureRepository>(); // <- legg til
    return services;
}
```

### 2. Registrer service i `AddApplicationServices()`

```csharp
public static IServiceCollection AddApplicationServices(this IServiceCollection services)
{
    services.AddScoped<IAuthService, AuthService>();
    services.AddScoped<IUserService, UserService>();
    services.AddScoped<IMinFeatureService, MinFeatureService>(); // <- legg til
    return services;
}
```

## Regler

- Bruk `AddScoped` for repositories og services — én instans per HTTP-request
- Bruk aldri `AddSingleton` for klasser som holder tilstand eller bruker `AppDbContext`
