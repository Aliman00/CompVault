using CompVault.Backend.Domain.Entities.Auth;
using CompVault.Backend.Domain.Entities.Documents;
using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Infrastructure.Auth;
using CompVault.Backend.Infrastructure.Data;
using CompVault.Backend.Tests.Common.Constants;
using CompVault.Shared.Constants;
using CompVault.Shared.Enums;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CompVault.Backend.Tests.Common;

/// <summary>
/// Oppretter ApplicationUsers for testing, og seeder inne i InMemory-databaser
/// </summary>
public static class TestDataSeeder
{
    // -------------------------------------------------------------------------
    // Database
    // -------------------------------------------------------------------------
    /// <summary>
    /// Sletter en eksisterende database, og oppretter en ny database mellom hver integrasjonstest
    /// Legger til en aktiv og en inaktiv bruker ved oppstart
    /// Trenger som regel alltid en bruker, men kan hende det er best å dele denne opp i en metode for å opprette
    /// databasen og en for å opprette brukere
    /// </summary>
    public static async Task CreateDb(IServiceProvider serviceProvider)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Nuker databasen og oppretter en ny database for hver integrasjonstest
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
    }

    // -------------------------------------------------------------------------
    // Users
    // -------------------------------------------------------------------------

    /// <summary>
    /// Oppretter og seeder en bruker i databas med en rolle
    /// Kaller CreateApplicationUser som en wrapper som lagrer med context
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="id">ID til en bruker hvis man trenger å slå opp ID for testing</param>
    /// <param name="email">Optional string med Epost for å opprette forskjellige brukere</param>
    /// <param name="deletedAt">DateTime som bestemmer om brukeren er aktive/slettet</param>
    /// <param name="role"></param>
    /// <returns>En opprettet ApplicationUser som er seedet i databasen</returns>
    public static async Task<ApplicationUser> SeedUserAsync(IServiceProvider serviceProvider, Guid? id = null,
        string email = TestConstants.Users.DefaultEmailForActiveUser, DateTime? deletedAt = null,
        string role = TestConstants.Roles.Default)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        UserManager<ApplicationUser> userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        RoleManager<ApplicationRole> roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        // Opprett rollen hvis den ikke eksisterer
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new ApplicationRole { Name = role });

        ApplicationUser user = TestDataFactory.CreateApplicationUser(id, email, deletedAt);
        await userManager.CreateAsync(user);
        await userManager.AddToRoleAsync(user, role);

        // Seed permissions and role-permissions for the user's role
        await SeedPermissionsAsync(serviceProvider);
        await SeedRolePermissionsForRoleAsync(serviceProvider, role);

        return user;
    }

    /// <summary>
    /// Seeds all permissions into the database. Mirrors DatabaseSeeder.SeedPermissionsAsync.
    /// </summary>
    public static async Task SeedPermissionsAsync(IServiceProvider serviceProvider)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        (string Name, string Description, string Category)[] permissions =
        [
            (Permissions.UsersRead, "Se brukere", "Users"),
            (Permissions.UsersWrite, "Opprett/endre brukere", "Users"),
            (Permissions.UsersDelete, "Slett brukere", "Users"),
            (Permissions.RolesRead, "Se roller", "Roles"),
            (Permissions.RolesWrite, "Opprett/endre roller", "Roles"),
            (Permissions.RolesDelete, "Slett roller", "Roles"),
            (Permissions.DepartmentsRead, "Se avdelinger", "Departments"),
            (Permissions.DepartmentsWrite, "Opprett/endre avdelinger", "Departments"),
            (Permissions.DepartmentsDelete, "Slett avdelinger", "Departments"),
            (Permissions.CompetenciesRead, "Se kompetanser", "Competencies"),
            (Permissions.CompetenciesWrite, "Opprett/endre kompetanser", "Competencies"),
            (Permissions.CompetenciesDelete, "Slett kompetanser", "Competencies"),
            (Permissions.DocumentTypesRead, "Se dokumenttyper", "DocumentTypes"),
            (Permissions.DocumentTypesWrite, "Opprett/endre dokumenttyper", "DocumentTypes"),
            (Permissions.DocumentTypesDelete, "Slett dokumenttyper", "DocumentTypes"),
            (Permissions.DocumentsRead, "Se dokumenter", "Documents"),
            (Permissions.DocumentsWrite, "Opprett/endre dokumenter", "Documents"),
            (Permissions.DocumentsDelete, "Slett dokumenter", "Documents"),
            (Permissions.DocumentsSign, "Signere dokumenter", "Documents"),
        ];

        foreach ((string name, string description, string category) in permissions)
        {
            bool exists = await context.Permissions.AnyAsync(p => p.Name == name);
            if (exists)
                continue;

            Permission permission = new()
            {
                Name = name,
                Description = description,
                Category = category
            };
            context.Permissions.Add(permission);
        }

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Seeds permissions for a specific role. This mirrors what DatabaseSeeder does in production.
    /// </summary>
    public static async Task SeedRolePermissionsForRoleAsync(IServiceProvider serviceProvider, string roleName)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Get the role
        ApplicationRole? role = await context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
        if (role == null)
            return;

        // Define permissions based on role
        string[] permissionNames = roleName switch
        {
            TestConstants.Roles.Admin => new[]
            {
                Permissions.CompetenciesRead,
                Permissions.CompetenciesWrite,
                Permissions.CompetenciesDelete,
                Permissions.DocumentTypesRead,
                Permissions.DocumentTypesWrite,
                Permissions.DocumentTypesDelete,
                Permissions.DocumentsRead,
                Permissions.DocumentsWrite,
                Permissions.DocumentsDelete,
                Permissions.DocumentsSign,
            },
            TestConstants.Roles.Default => new[]
            {
                Permissions.CompetenciesRead,
            },
            _ => Array.Empty<string>()
        };

        // Get all permissions from DB
        var allPermissions = context.Set<Permission>().ToList();

        foreach (string permName in permissionNames)
        {
            Permission? permission = allPermissions.FirstOrDefault(p => p.Name == permName);
            if (permission == null)
                continue;

            // Check if already exists
            bool exists = await context.RolePermissions.AnyAsync(
                rp => rp.RoleId == role.Id && rp.PermissionId == permission.Id);
            if (exists)
                continue;

            context.RolePermissions.Add(new RolePermission
            {
                RoleId = role.Id,
                PermissionId = permission.Id,
                GrantedAt = DateTime.UtcNow,
            });
        }

        await context.SaveChangesAsync();
    }

    // -------------------------------------------------------------------------
    // OTP
    // -------------------------------------------------------------------------


    /// <summary>
    /// Seeder en Otp-kode inn i databasen. Har optional felt på alle egenskapene som er verdt å teste
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="userId">Brukeren som Otp-koden tilhører. Default til ActiveUserId</param>
    /// <param name="plainTextCode">Koden i plaintext som blir hashet i metoden. Default konstant</param>
    /// <param name="createdAt">Når OTP-koden er opprettet. Defauklt UtcNop</param>
    /// <param name="expiresAt">DateTime-objekt som spesifiserer når den går ut. Default om 10 min</param>
    /// <param name="failedAttempts">Antall feilede forsøk. Default = 0</param>
    /// <param name="isUsed">Setter om OTP-koden er brukt eller ikke. Default = false</param>
    /// <returns>Opprettet OtpCode</returns>
    public static async Task<OtpCode> SeedOtpCodeAsync(IServiceProvider serviceProvider, Guid? userId = null,
        string plainTextCode = TestConstants.Otp.PlainTextOtpCode, DateTime? createdAt = null,
        DateTime? expiresAt = null, int failedAttempts = 0, bool isUsed = false)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        OtpCode otpCode = TestDataFactory.CreateOtpCode(userId: userId,
            plainTextCode: plainTextCode, createdAt: createdAt, expiresAt: expiresAt, failedAttempts: failedAttempts,
            isUsed: isUsed);

        context.Set<OtpCode>().Add(otpCode);
        await context.SaveChangesAsync();

        return otpCode;
    }

    /// <summary>
    /// Oppretter og seeder en RefreshToken inne i databasen
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="userId">Brukeren som Token tilhører. Default ActiveUserId</param>
    /// <param name="token">Selve token, kun en enkel string i testene. Default token-konstant</param>
    /// <param name="createdAt">Når den er opprettet. Default UtcNow</param>
    /// <param name="expiresAt">Når den utgår. Default om 15 min fra opprettelse</param>
    /// <param name="isRevoked">Bool på om koden er gyldig eller revoked</param>
    /// <returns>En opprettet RefreshToken</returns>
    public static async Task<RefreshToken> SeedRefreshTokenAsync(IServiceProvider serviceProvider,
        Guid? userId = null, string? token = null,
        DateTime? createdAt = null, DateTime? expiresAt = null, bool isRevoked = false)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        RefreshToken refreshToken = TestDataFactory.CreateRefreshToken(
            userId: userId ?? TestConstants.Users.ActiveUserId,
            token: token,
            createdAt: createdAt,
            expiresAt: expiresAt,
            isRevoked: isRevoked);

        context.Set<RefreshToken>().Add(refreshToken);
        await context.SaveChangesAsync();
        return refreshToken;
    }

    // -------------------------------------------------------------------------
    // Http
    // -------------------------------------------------------------------------

    /// <summary>
    /// Oppretter en HttpClient med et gyldig JWT-token for brukeren med den oppgitte ID-en.
    /// Genererer tokenet direkte via JwtService — ingen OTP-flyt nødvendig.
    /// Brukeren må være seedet i databasen før denne metoden kalles.
    /// </summary>
    /// <param name="factory">WebApplicationFactory som brukes til å opprette HttpClient og hente tjenester.</param>
    /// <param name="userId">ID til brukeren tokenet skal tilhøre. Default til ActiveUserId.</param>
    /// <returns>HttpClient med Authorization-header satt til brukerens token.</returns>
    public static async Task<HttpClient> CreateAuthenticatedClientAsync(
        WebApplicationFactory<Program> factory,
        Guid? userId = null)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        UserManager<ApplicationUser> userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        IJwtService jwtService = scope.ServiceProvider.GetRequiredService<IJwtService>();
        IPermissionService permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();

        ApplicationUser user = await userManager.FindByIdAsync((userId ?? TestConstants.Users.ActiveUserId).ToString())
            ?? throw new InvalidOperationException("Bruker ikke funnet — seed brukeren før du kaller CreateAuthenticatedClientAsync.");

        IList<string> roles = await userManager.GetRolesAsync(user);
        IList<string> permissions = await permissionService.GetPermissionsForRolesAsync(roles, CancellationToken.None);
        string token = jwtService.GenerateAccessToken(user, roles, permissions);

        HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        return client;
    }

    // -------------------------------------------------------------------------
    // Document Types, Categories and Documents
    // -------------------------------------------------------------------------

    /// <summary>
    /// Seeds document types, categories and documents into the test database.
    /// </summary>
    public static async Task SeedDocumentDataAsync(IServiceProvider serviceProvider)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Seed document types
        DocumentType[] documentTypes = new[]
        {
            new DocumentType
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "HMS Dokumenter",
                Slug = "hms-documents",
                Description = "Helse-, miljø- og sikkerhetsdokumenter",
                TargetMode = DocumentTargetMode.Department,
                StorageFolder = "hms-documents",
                AllowedMimeTypes = ["application/pdf"],
                MaxFileSizeBytes = 20 * 1024 * 1024,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new DocumentType
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "Kursmateriell",
                Slug = "course-materials",
                Description = "Kursmateriell og opplæringsdokumenter",
                TargetMode = DocumentTargetMode.None,
                StorageFolder = "course-materials",
                AllowedMimeTypes = ["application/pdf"],
                MaxFileSizeBytes = 20 * 1024 * 1024,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
        };

        foreach (DocumentType? dt in documentTypes)
        {
            if (!await context.DocumentTypes.AnyAsync(d => d.Slug == dt.Slug))
            {
                context.DocumentTypes.Add(dt);
            }
        }
        await context.SaveChangesAsync();

        // Seed document type categories
        DocumentTypeCategory[] categories = new[]
        {
            new DocumentTypeCategory
            {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                DocumentTypeId = documentTypes[0].Id,
                Name = "Nødsprosedyrer",
                Slug = "emergency-procedure",
                IsActive = true
            },
            new DocumentTypeCategory
            {
                Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                DocumentTypeId = documentTypes[0].Id,
                Name = "Sikkerhetsinstrukser",
                Slug = "safety-instruction",
                IsActive = true
            },
        };

        foreach (DocumentTypeCategory? cat in categories)
        {
            if (!await context.DocumentTypeCategories.AnyAsync(c => c.Slug == cat.Slug))
            {
                context.DocumentTypeCategories.Add(cat);
            }
        }
        await context.SaveChangesAsync();

        // Seed documents (UploadedBy set to a placeholder GUID - will be linked when users are seeded)
        Document[] documents = new[]
        {
            new Document
            {
                Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                DocumentTypeId = documentTypes[0].Id,
                DocumentTypeCategoryId = categories[0].Id,
                Title = "Brannverninstruks",
                RequiresSignature = true,
                Version = 1,
                IsCurrent = true,
                IsActive = true,
                UploadedBy = Guid.Empty,
                UploadedAt = DateTime.UtcNow
            },
            new Document
            {
                Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                DocumentTypeId = documentTypes[1].Id,
                Title = "Onboarding-guide",
                RequiresSignature = false,
                Version = 1,
                IsCurrent = true,
                IsActive = true,
                UploadedBy = Guid.Empty,
                UploadedAt = DateTime.UtcNow
            },
        };

        foreach (Document? doc in documents)
        {
            if (!await context.Documents.AnyAsync(d => d.Title == doc.Title))
            {
                context.Documents.Add(doc);
            }
        }
        await context.SaveChangesAsync();
    }
}