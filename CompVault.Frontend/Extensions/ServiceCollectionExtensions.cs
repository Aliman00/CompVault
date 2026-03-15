using CompVault.Frontend.Features.Auth.Services;

namespace CompVault.Frontend.Extensions;

/// <summary>
/// Extension-metoder på <see cref="IServiceCollection"/> som grupperer service-registreringer i Frontend.
/// Kalles fra Program.cs for å holde alt ryddig.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFrontendServices(this IServiceCollection services)
    {
        // services.AddScoped<IAuthService, AuthService>();
        
        return services;
    }
}