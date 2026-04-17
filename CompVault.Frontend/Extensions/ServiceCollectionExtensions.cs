using CompVault.Frontend.Common.Configuration;
using CompVault.Frontend.Common.Constants;
using CompVault.Frontend.Common.Http;
using CompVault.Frontend.Common.Localization;
using CompVault.Frontend.Common.Services;
using CompVault.Frontend.Dev;
using CompVault.Frontend.Features.Auth.Services;
using CompVault.Frontend.Features.Departments.Services;
using CompVault.Frontend.Features.JobTitle.Services;
using CompVault.Frontend.Features.Users.Services;
using CompVault.Shared.Constants;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.DependencyInjection.Extensions;

using MudBlazor;

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

        // Registrer handleren som Scoped så den får riktig HttpContext per krets
        services.AddScoped<AccessTokenHandler>();

        // Hovedklienten med handler for autentisering — brukes av alle vanlige kall
        services.AddHttpClient(BackendApiSettings.MainClientName, client =>
        {
            client.BaseAddress = new Uri(settings.BaseUrl);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        })
        .AddHttpMessageHandler<AccessTokenHandler>();

        // Anonymklient uten Bearer — brukes kun til refresh i OnValidatePrincipal
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
    public static IServiceCollection AddAuth(this IServiceCollection services, IConfiguration configuration,
        IWebHostEnvironment env)
    {
        AuthSettings settings = configuration
            .GetSection(AuthSettings.SectionName)
            .Get<AuthSettings>() ?? new AuthSettings();

        // Gjør den tilgjengelig for LoginCallback SSR
        services.AddSingleton(settings);

        // Vi må registrere denne for å hente ut instanser som brukes av den aktive kretsen
        services.AddHttpContextAccessor();

        services.AddScoped<CircuitHandler, CircuitUserContextHandler>();
        services.AddScoped<CircuitUserContext>();
        services.AddScoped<CookieValidationEvents>();

        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/login";
                options.LogoutPath = "/logout";
                options.ExpireTimeSpan = TimeSpan.FromDays(settings.CookieExpireDays);
                options.SlidingExpiration = false;
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.SecurePolicy = env.IsDevelopment()
                    ? CookieSecurePolicy.SameAsRequest
                    : CookieSecurePolicy.Always;

                options.EventsType = typeof(CookieValidationEvents);
            });

        services.AddScoped<AuthStateProvider>();
        services.AddSingleton<ITokenRefreshService, TokenRefreshService>();

        // Forteller Blazor at vår egen AuthStateProvider brukes
        services.AddScoped<AuthenticationStateProvider>(
            sp => sp.GetRequiredService<AuthStateProvider>());

        services.AddScoped<IAuthService, AuthService>();

        return services;
    }

    /// <summary>
    /// Legger til autorisasjon og policies i Blazor
    /// </summary>
    public static IServiceCollection AddAuthPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {   
            // Admin-panel tilgang 
            options.AddPolicy(Permissions.AdminAccess, policy =>
                policy.RequireClaim(Permissions.ClaimType, Permissions.AdminAccess));
            
            // Users
            options.AddPolicy(Permissions.UsersRead, policy =>
                policy.RequireClaim(Permissions.ClaimType, Permissions.UsersRead));
            options.AddPolicy(Permissions.UsersWrite, policy =>
                policy.RequireClaim(Permissions.ClaimType, Permissions.UsersWrite));
            options.AddPolicy(Permissions.UsersDelete, policy =>
                policy.RequireClaim(Permissions.ClaimType, Permissions.UsersDelete));
            
            // Roles
            options.AddPolicy(Permissions.RolesRead, policy =>
                policy.RequireClaim(Permissions.ClaimType, Permissions.RolesRead));
            options.AddPolicy(Permissions.RolesWrite, policy =>
                policy.RequireClaim(Permissions.ClaimType, Permissions.RolesWrite));
            options.AddPolicy(Permissions.RolesDelete, policy =>
                policy.RequireClaim(Permissions.ClaimType, Permissions.RolesDelete));
            
            // Department
            options.AddPolicy(Permissions.DepartmentsRead, policy =>
                policy.RequireClaim(Permissions.ClaimType, Permissions.DepartmentsRead));
            options.AddPolicy(Permissions.DepartmentsWrite, policy =>
                policy.RequireClaim(Permissions.ClaimType, Permissions.DepartmentsWrite));
            options.AddPolicy(Permissions.DepartmentsDelete, policy =>
                policy.RequireClaim(Permissions.ClaimType, Permissions.DepartmentsDelete));
            
            // Competencies
            options.AddPolicy(Permissions.CompetenciesRead, policy =>
                policy.RequireClaim(Permissions.ClaimType, Permissions.CompetenciesRead));
            options.AddPolicy(Permissions.CompetenciesWrite, policy =>
                policy.RequireClaim(Permissions.ClaimType, Permissions.CompetenciesWrite));
            options.AddPolicy(Permissions.CompetenciesDelete, policy =>
                policy.RequireClaim(Permissions.ClaimType, Permissions.CompetenciesDelete));
        });

        return services;
    }

    /// <summary>
    /// Legger til frontend servicer - eksempel er API-Services som AuthService
    /// </summary>
    public static IServiceCollection AddFrontendServices(this IServiceCollection services, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
            services.AddScoped<IDevService, DevService>();

        // ================================ Infrastruktur ================================
        services.AddScoped<IThemeService, ThemeService>();
        services.AddLocalization();
        services.AddTransient<MudLocalizer, NorwegianMudLocalizer>();
        
        // ================================ Admin forretningslogikk ================================
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<IJobTitleService, JobTitleService>();


        return services;
    }
}