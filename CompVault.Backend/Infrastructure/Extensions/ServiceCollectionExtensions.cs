using System.Text;

using CompVault.Backend.Common.Middleware;
using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Features.Auth.Configuration;
using CompVault.Backend.Features.Auth.Services;
using CompVault.Backend.Features.Users.Services;
using CompVault.Backend.Infrastructure.Auth;
using CompVault.Backend.Infrastructure.Data;
using CompVault.Backend.Infrastructure.Email;
using CompVault.Backend.Infrastructure.Email.Config;
using CompVault.Backend.Infrastructure.Jobs;
using CompVault.Backend.Infrastructure.Repositories.Auth;
using CompVault.Backend.Infrastructure.Repositories.Identity;

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
    /// Ved testing så brukes ikke PostgreSQL med UseNpgsql, men InMemory.
    /// </summary>
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        // Skipper oppsett av PostgreSQL hvis vi er i testing environment
        if (!environment.IsEnvironment("Testing"))
        {
            DatabaseSettings dbSettings = configuration
                .GetSection(DatabaseSettings.SectionName)
                .Get<DatabaseSettings>() ?? throw new InvalidOperationException("Database-konfigurasjon mangler.");

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(
                    dbSettings.BuildConnectionString(),
                    npgsql => npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));
        }

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
        IConfigurationSection jwtSection = configuration.GetSection(JwtSettings.SectionName);

        services.Configure<JwtSettings>(jwtSection);
        services.Configure<OtpOptions>(configuration.GetSection(OtpOptions.SectionName));

        JwtSettings jwtSettings = jwtSection
            .Get<JwtSettings>() ?? throw new InvalidOperationException("JWT-konfigurasjon mangler.");

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
                    // Fjerner standard 5-minutters slingringsmonn slik at tokens utløper nøyaktig når de skal
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
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        // Rydder opp utgåtte og revokerte refresh tokens én gang i døgnet
        services.AddHostedService<TokenCleanupJob>();

        return services;
    }

    /// <summary>
    /// Konfigurerer e-post med Resend. Hoppes over i Testing-miljøet.
    /// </summary>
    public static IServiceCollection AddEmail(this IServiceCollection services, IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        // EmailService mockes i integrasjonstester — hopper over oppsett i Testing-miljøet
        if (environment.IsEnvironment("Testing"))
            return services;

        EmailSettings emailSettings = configuration
            .GetSection(EmailSettings.SectionName)
            .Get<EmailSettings>() ?? throw new InvalidOperationException("E-postkonfigurasjon mangler.");

        if (string.IsNullOrEmpty(emailSettings.ApiKey))
            throw new InvalidOperationException("Email:ApiKey er ikke konfigurert.");

        if (string.IsNullOrWhiteSpace(emailSettings.FromAddress))
            throw new InvalidOperationException("Email:FromAddress er ikke konfigurert.");

        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
        services.Configure<ResendClientOptions>(resendOptions => resendOptions.ApiToken = emailSettings.ApiKey);
        services.AddHttpClient<IResend, ResendClient>();
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
        services.AddScoped<IOtpCodeRepository, OtpCodeRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        return services;
    }

    /// <summary>
    /// Registrerer alle applikasjonsservicene med scoped levetid.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IOtpCodeService, OtpCodeService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();

        return services;
    }
}