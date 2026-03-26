using CompVault.Frontend.Common.Configuration;
using CompVault.Frontend.Common.Services;
using CompVault.Frontend.Dev;
using CompVault.Frontend.Features.Auth.Services;

using Microsoft.AspNetCore.Components.Authorization;

namespace CompVault.Frontend.Extensions;

/// <summary>
/// Extension-metoder på <see cref="IServiceCollection"/> som grupperer service-registreringer i Frontend.
/// Kalles fra Program.cs for å holde alt ryddig.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Oppretter HttpClienter - har kun en mot backend for øyeblikket
    /// </summary>
    public static IServiceCollection AddHttpClients(this IServiceCollection services, IConfiguration configuration)
    {
        BackendApiSettings settings = configuration
                           .GetSection(BackendApiSettings.SectionName)
                           .Get<BackendApiSettings>()
                       ?? throw new InvalidOperationException("BackendApi does not exist in appsettings");

        if (string.IsNullOrWhiteSpace(settings.BaseUrl))
            throw new InvalidOperationException("BackendApi:BaseUrl does not exist in appsettings");
        
        // Oppretter kun en handler pr HttpClient-kall
        services.AddScoped<AuthTokenHandler>();
        
        // Hovedklienten med handler for autentisering — brukes av alle vanlige kall
        services.AddHttpClient(BackendApiSettings.MainClientName, client =>
            {
                client.BaseAddress = new Uri(settings.BaseUrl);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            })
            .AddHttpMessageHandler<AuthTokenHandler>();
        
        // Klient som brukes kun av for refresh av token
        services.AddHttpClient(BackendApiSettings.AuthClientName, client =>
        {
            client.BaseAddress = new Uri(settings.BaseUrl);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        return services;
    }
    
    
    /// <summary>
    /// Legger til autentisering for Blazor Server
    /// </summary>
    public static IServiceCollection AddAuth(this IServiceCollection services)
    {
        // Vi må registrere denne for å hente ut instanser som brukes av den aktive kretsen
        services.AddHttpContextAccessor();
        
        services.AddScoped<TokenProvider>();
        services.AddScoped<AuthStateProvider>();

        // Forteller Blazor at vår egen AuthStateProvider brukes
        services.AddScoped<AuthenticationStateProvider>(
            sp => sp.GetRequiredService<AuthStateProvider>());
        
        services.AddScoped<IAuthService, AuthService>();
        
        return services;
    }

    /// <summary>
    /// Legger til frontend servicer - eksempel er API-Services som AuthService
    /// </summary>
    public static IServiceCollection AddFrontendServices(this IServiceCollection services, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
            services.AddScoped<IDevService, DevService>();

        return services;
    }
}