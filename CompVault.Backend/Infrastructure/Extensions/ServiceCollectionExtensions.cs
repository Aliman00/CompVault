using System.Text;

using CompVault.Backend.Common.Middleware;
using CompVault.Backend.Common.Responses;
using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Features.Auth.Configuration;
using CompVault.Backend.Features.Auth.Services;
using CompVault.Backend.Features.Competencies.Services;
using CompVault.Backend.Features.Departments.Services;
using CompVault.Backend.Features.Documents.Services;
using CompVault.Backend.Features.JobTitles.Services;
using CompVault.Backend.Features.Equipment.Services;
using CompVault.Backend.Features.Roles.Services;
using CompVault.Backend.Features.Users.Services;
using CompVault.Backend.Features.Audit.Services;
using CompVault.Backend.Infrastructure.Auth;
using CompVault.Backend.Infrastructure.Configuration;
using CompVault.Backend.Infrastructure.Data;
using CompVault.Backend.Infrastructure.Data.Interceptors;
using CompVault.Backend.Infrastructure.Email;
using CompVault.Backend.Infrastructure.Email.Config;
using CompVault.Backend.Infrastructure.FileStorage;
using CompVault.Backend.Infrastructure.FileStorage.Configuration;
using CompVault.Backend.Infrastructure.Jobs;
using CompVault.Backend.Infrastructure.Repositories.Auth;
using CompVault.Backend.Infrastructure.Repositories.Competencies;
using CompVault.Backend.Infrastructure.Repositories.Departments;
using CompVault.Backend.Infrastructure.Repositories.Documents;
using CompVault.Backend.Infrastructure.Repositories.Identity;
using CompVault.Backend.Infrastructure.Repositories.JobTitles;
using CompVault.Backend.Infrastructure.Repositories.Equipment;
using CompVault.Shared.Constants;
using CompVault.Shared.Result;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
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

            services.AddDbContext<AppDbContext>((sp, options) =>
            {
                options.UseNpgsql(
                    dbSettings.BuildConnectionString(),
                    npgsql => npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));
                options.AddInterceptors(
                    new AuditSaveChangesInterceptor(sp),
                    new DepartmentScopeSaveChangesInterceptor(sp));
            });
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

        // registerer først uten konfigurasjon for å kunne fungere med testene
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
        .Configure<IOptions<JwtSettings>>((jwtOpts, settings) =>
        {
            JwtSettings jwt = settings.Value;

            jwtOpts.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwt.Issuer,
                ValidateAudience = true,
                ValidAudience = jwt.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwt.Secret)),
                ValidateLifetime = true,
                // Fjerner standard 5-minutters slingringsmonn slik at tokens utløper nøyaktig når de skal
                ClockSkew = TimeSpan.Zero
            };

            jwtOpts.Events = new JwtBearerEvents
            {
                OnChallenge = context =>
                {
                    context.HandleResponse();

                    if (context.Response.HasStarted)
                        return Task.CompletedTask;

                    string message = context.AuthenticateFailure?.Message ??
                        ProblemDetailBuilder.GetDefaultMessage(ErrorCode.Unauthorized);

                    ProblemDetail problem = ProblemDetailBuilder.Create(
                        401, ErrorCode.Unauthorized.ToString(), message);

                    context.Response.StatusCode = problem.Status;
                    context.Response.ContentType = "application/problem+json";
                    return context.Response.WriteAsJsonAsync(problem);
                },
                OnAuthenticationFailed = context =>
                {
                    ILogger? logger = context.HttpContext.RequestServices.GetService<ILogger>();
                    logger?.LogWarning(
                        context.Exception,
                        "JWT authentication failed: {Error}",
                        context.Exception?.Message ?? "Unknown error");
                    return Task.CompletedTask;
                }
            };
        });

        services.AddAuthorization(options =>
        {
            // Dynamisk registrering av policies basert på Permissions.cs-konstanter
            typeof(Permissions)
                .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.DeclaredOnly)
                .Where(f => f.FieldType == typeof(string))
                .Select(f => (string)f.GetValue(null)!)
                .ToList()
                .ForEach(permission =>
                {
                    options.AddPolicy(permission, policy =>
                        policy.RequireClaim(Permissions.ClaimType, permission));
                });
        });
        services.AddScoped<IJwtService, JwtService>();

        return services;
    }

    /// <summary>
    /// Konfigurerer CORS slik at frontend kan sende cookies og autentiserte forespørsler til backend
    /// </summary>
    public static IServiceCollection AddFrontendCors(this IServiceCollection services, IConfiguration configuration)
    {
        CorsSettings corsSettings = configuration
                                        .GetSection(CorsSettings.SectionName)
                                        .Get<CorsSettings>()
                                    ?? throw new InvalidOperationException("CORS-konfigurasjon mangler.");

        services.AddCors(options =>
        {
            options.AddPolicy(CorsSettings.PolicyName, policy =>
            {
                policy
                    .WithOrigins(corsSettings.GetOrigins())
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        return services;
    }

    /// <summary>
    /// Legger til generell infrastruktur
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
        services.AddHttpContextAccessor();

        if (!environment.IsEnvironment("Testing")) // Trenger ikke bakgrunns jobber under testing
        {
            // Rydder opp utgåtte og revokerte refresh tokens én gang i døgnet
            services.AddHostedService<TokenCleanupJob>();

            // Beregner status på kompetansebevis én gang i døgnet
            services.AddHostedService<CompetencyStatusJob>();
        }

        // Fillagring
        services.Configure<FileStorageSettings>(configuration.GetSection(nameof(FileStorageSettings)));
        services.AddScoped<IFileStorageService, LocalFileStorageService>();

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
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<ICompetencyTypeRepository, CompetencyTypeRepository>();
        services.AddScoped<ICompetencyRepository, CompetencyRepository>();
        services.AddScoped<IOtpCodeRepository, OtpCodeRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        // Documents
        services.AddScoped<IDocumentTypeRepository, DocumentTypeRepository>();
        services.AddScoped<IDocumentTypeCategoryRepository, DocumentTypeCategoryRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IDocumentSignatureRepository, DocumentSignatureRepository>();

        // JobTitles
        services.AddScoped<IJobTitleRepository, JobTitleRepository>();

        // Equipment
        services.AddScoped<IEquipmentCategoryRepository, EquipmentCategoryRepository>();
        services.AddScoped<IEquipmentItemRepository, EquipmentItemRepository>();
        services.AddScoped<IEquipmentIssuanceRepository, EquipmentIssuanceRepository>();

        return services;
    }

    /// <summary>
    /// Registrerer alle applikasjonsservicene med scoped levetid.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Hierarki-sjekk
        services.AddScoped<IDepartmentScopeService, DepartmentScopeService>();
        
        // Audit
        services.AddScoped<IAuditContext, AuditContext>();
        services.AddScoped<IAuditLogService, AuditLogService>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<ICompetencyTypeService, CompetencyTypeService>();
        services.AddScoped<ICompetencyService, CompetencyService>();
        services.AddScoped<IOtpCodeService, OtpCodeService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IRoleService, RoleService>();

        // Documents
        services.AddScoped<IDocumentTypeService, DocumentTypeService>();
        services.AddScoped<IDocumentFileService, DocumentFileService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IDocumentTargetingService, DocumentTargetingService>();
        services.AddScoped<IDocumentVersioningService, DocumentVersioningService>();
        services.AddScoped<IDocumentSignatureService, DocumentSignatureService>();

        // JobTitles
        services.AddScoped<IJobTitleService, JobTitleService>();

        // Equipment
        services.AddScoped<IEquipmentCategoryService, EquipmentCategoryService>();
        services.AddScoped<IEquipmentItemService, EquipmentItemService>();
        services.AddScoped<IEquipmentIssuanceService, EquipmentIssuanceService>();

        return services;
    }
}