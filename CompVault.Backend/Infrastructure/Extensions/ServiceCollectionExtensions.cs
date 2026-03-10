using System.Text;
using CompVault.Backend.Common.Middleware;
using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Features.Auth;
using CompVault.Backend.Features.Users;
using CompVault.Backend.Infrastructure.Auth;
using CompVault.Backend.Infrastructure.Data;
using CompVault.Backend.Infrastructure.Data.Repositories.Identity;
using CompVault.Backend.Infrastructure.Email;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Resend;

namespace CompVault.Backend.Infrastructure.Extensions;

/// <summary>
/// Extension-metoder på <see cref="IServiceCollection"/> som grupperer service-registreringer.
/// Kalles fra Program.cs for å holde alt ryddig.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Setter opp databasekoblingen med Npgsql og registrerer ASP.NET Core Identity.
    /// </summary>
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("Default"),
                npgsql => npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        services.AddIdentityCore<ApplicationUser>(opts =>
            {
                // Passordkrav er deaktivert — systemet bruker passordløs OTP-autentisering.
                // Identity krever fortsatt at passordreglene er satt, men vi minimerer dem
                // slik at CreateAsync(user) uten passord ikke feiler.
                opts.Password.RequireDigit = false;
                opts.Password.RequiredLength = 0;
                opts.Password.RequireUppercase = false;
                opts.Password.RequireNonAlphanumeric = false;
                opts.Password.RequireLowercase = false;
                opts.Password.RequiredUniqueChars = 0;

                opts.User.RequireUniqueEmail = true;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<AppDbContext>();

        return services;
    }

    /// <summary>
    /// Konfigurerer JWT-autentisering og binder <see cref="JwtSettings"/> fra appsettings.
    /// </summary>
    public static IServiceCollection AddAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        JwtSettings jwtSettings = configuration
            .GetSection(JwtSettings.SectionName)
            .Get<JwtSettings>() ?? new JwtSettings();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opts =>
            {
                opts.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization();
        services.AddScoped<IJwtService, JwtService>();

        return services;
    }
    
    /// <summary>
    /// Legger til generell infrastruktur
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // ============ ERROR HANDLING ============
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
        
        return services;
    }
    
    /// <summary>
    /// Konfigurerer Epost med Resend
    /// </summary>
    public static IServiceCollection AddEmail(this IServiceCollection services, IConfiguration configuration)
    {
        // Email Service Resend
        var resendApiKey = configuration["Email:ApiKey"]
                           ?? throw new InvalidOperationException(
                               "Email:ApiKey is not configured. " +
                               "Add RESEND_API_KEY to environment variables or appsettings.json");
        
        // Register Resend options
        services.Configure<ResendClientOptions>(o => o.ApiToken = resendApiKey);
        
        // HttpClient for Resend
        services.AddHttpClient<IResend, ResendClient>();
        
        // Registerer EmailService som scoped
        services.AddScoped<IEmailService, EmailService>();

        return services;
    }

    /// <summary>
    /// Registrerer alle repository-implementasjoner og Unit of Work med scoped levetid.
    /// </summary>
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();

        return services;
    }

    /// <summary>
    /// Registrerer alle applikasjonsservicene med scoped levetid.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();

        return services;
    }
}
