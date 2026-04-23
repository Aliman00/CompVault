using System.Reflection;

using CompVault.Frontend.Common.Configuration;
using CompVault.Frontend.Common.Http;
using CompVault.Frontend.Common.Localization;
using CompVault.Frontend.Common.Services;
using CompVault.Frontend.Dev;
using CompVault.Frontend.Features.Auth.Services;
using CompVault.Frontend.Features.Competencies.Services;
using CompVault.Frontend.Features.Departments.Services;
using CompVault.Frontend.Features.Documents.Services;
using CompVault.Frontend.Features.Equipment.Services;
using CompVault.Frontend.Features.JobTitle.Services;
using CompVault.Frontend.Features.Roles.Services;
using CompVault.Frontend.Features.Users.Services;
using CompVault.Shared.Constants;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;

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
        // Forteller Blazor at vår egen AuthStateProvider brukes
        services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<AuthStateProvider>());

        services.AddSingleton<ITokenRefreshService, TokenRefreshService>();
        services.AddScoped<IClaimsRefreshService, ClaimsRefreshService>();

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
            typeof(Permissions)
                .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(f => f.FieldType == typeof(string) && f.Name != nameof(Permissions.ClaimType))
                .Select(f => (string)f.GetValue(null)!)
                .ToList()
                .ForEach(permission =>
                {
                    options.AddPolicy(permission, policy =>
                        policy.RequireClaim(Permissions.ClaimType, permission));
                });
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
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<ICompetencyService, CompetencyService>();
        services.AddScoped<ICompetencyTypeService, CompetencyTypeService>();
        services.AddScoped<IJobTitleService, JobTitleService>();
        services.AddScoped<IDocumentTypeService, DocumentTypeService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IDocumentTypeCategoryService, DocumentTypeCategoryService>();
        services.AddScoped<ISignatureService, SignatureService>();
        services.AddScoped<IEquipmentCategoryService, EquipmentCategoryService>();
        services.AddScoped<IEquipmentItemService, EquipmentItemService>();
        services.AddScoped<IEquipmentIssuancesService, EquipmentIssuancesService>();

        return services;
    }
}