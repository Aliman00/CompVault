using CompVault.Frontend.Common.Configuration;
using CompVault.Frontend.Features.Auth.Services;

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
        var settings = configuration
                           .GetSection(BackendApiSettings.SectionName)
                           .Get<BackendApiSettings>() 
                       ?? throw new InvalidOperationException("BackendApi does not exist in appsettings");

        if (string.IsNullOrWhiteSpace(settings.BaseUrl))
            throw new InvalidOperationException("BackendApi:BaseUrl does not exist in appsettings");

        services.AddHttpClient("BackendApi", client =>
        {
            client.BaseAddress = new Uri(settings.BaseUrl);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        return services;
    }
    
    /// <summary>
    /// Legger til frontend servicer - eksempel er API-Services som AuthService
    /// </summary>
    public static IServiceCollection AddFrontendServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        
        return services;
    }
}