using CompVault.Frontend.Common.Configuration;
using CompVault.Frontend.Common.Services;
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

        services.AddHttpClient(BackendApiSettings.ClientName, client =>
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
    public static IServiceCollection AddFrontendServices(this IServiceCollection services)
    {
        

        return services;
    }
}