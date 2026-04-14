using CompVault.Backend.Domain.Entities.Competencies;
using CompVault.Backend.Domain.Entities.Departments;
using CompVault.Backend.Domain.Entities.Documents;
using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Domain.Entities.JobTitles;
using CompVault.Backend.Features.Competencies;
using CompVault.Backend.Infrastructure.Data;
using CompVault.Shared.Constants;
using CompVault.Shared.Enums;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CompVault.Backend.Dev;

/// <summary>
/// Seeder databasen med testdata for Development-miljøet.
/// ADVARSEL: Skal KUN kjøres i Development — fjern Dev/-mappen og seed-kallet i Program.cs før deploy.
/// </summary>
public static class DatabaseSeeder
{
    private const string DefaultPassword = "Test123!";

    private static readonly (string FirstName, string LastName, string Email, string[] Roles)[] Users =
    [
        ("Kari",   "Nordmann", "kari.nordmann@compvault.no", ["Admin"]),
        ("Ola",    "Nordmann", "ola.nordmann@compvault.no",  ["Admin"]),
        ("Lars",   "Hansen",   "lars.hansen@compvault.no",   ["Employee"]),
        ("Ingrid", "Berg",     "ingrid.berg@compvault.no",   ["Employee"]),
        ("Tobias", "Lie",      "tobias.lie@compvault.no",    ["Employee"]),
        ("Sofie",  "Dahl",     "sofie.dahl@compvault.no",    ["Employee"]),
        ("Almin",  "Colakovic","almin.dev@pm.me",            ["Employee"]),
        ("Majlinda","Lajci",   "gamingnerd824@gmail.com",    ["Employee"]),
        ("Fredrik","Magee",    "fredrik@magee.no",           ["Employee"]),
    ];

    // JobTitles: stillingstitler som seedes i systemet
    private static readonly string[] JobTitlesData =
    [
        "System Administrator",
        "IT-leder",
        "Systemutvikler",
        "Rådgiver",
        "HR-konsulent",
    ];

    // User -> JobTitle mapping: (UserEmail, JobTitleName)
    private static readonly (string UserEmail, string JobTitleName)[] UserJobTitles =
    [
        ("kari.nordmann@compvault.no", "System Administrator"),
        ("ola.nordmann@compvault.no",  "IT-leder"),
        ("lars.hansen@compvault.no",   "Systemutvikler"),
        ("ingrid.berg@compvault.no",   "Systemutvikler"),
        ("tobias.lie@compvault.no",    "Rådgiver"),
        ("sofie.dahl@compvault.no",    "HR-konsulent"),
        ("almin.dev@pm.me",            "Systemutvikler"),
        ("gamingnerd824@gmail.com",     "Systemutvikler"),
        ("fredrik@magee.no",           "Systemutvikler"),
    ];

    private static readonly (string Name, string Description)[] Roles =
    [
        ("Admin",    "Full tilgang til alle funksjoner i systemet."),
        ("Employee", "Standard ansatt-tilgang."),
    ];

    // Departments: (Name, Description, ParentDepartmentName)
    private static readonly (string Name, string Description, string? ParentDepartmentName)[] Departments =
    [
        ("Ledelse",         "Overordnet ledelse av selskapet.",                      null),
        ("IT",              "IT-avdelingen med ansvar for systemer og utvikling.",   null),
        ("Utvikling",       "Programmering og systemutvikling.",                     "IT"),
        ("Drift",           "Drift og vedlikehold av IT-infrastruktur.",             "IT"),
        ("HR",              "Human Resources og personaladministrasjon.",             null),
        ("Rekruttering",    "Rekruttering og ansettelsesprosesser.",                  "HR"),
    ];

    // User -> Department mapping: (UserEmail, DepartmentName)
    private static readonly (string UserEmail, string DepartmentName)[] UserDepartments =
    [
        ("kari.nordmann@compvault.no", "Ledelse"),
        ("ola.nordmann@compvault.no",  "IT"),
        ("lars.hansen@compvault.no",   "Utvikling"),
        ("ingrid.berg@compvault.no",   "Utvikling"),
        ("tobias.lie@compvault.no",    "Drift"),
        ("sofie.dahl@compvault.no",    "Rekruttering"),
        ("gamingnerd824@gmail.com",     "Utvikling"),
        ("fredrik@magee.no",           "Utvikling"),
        ("almin.dev@pm.me",           "Utvikling"),
    ];

    // CompetencyTypes: (Name, Description, Category, RequiresExpiration)
    private static readonly (string Name, string? Description, string? Category, bool RequiresExpiration)[] CompetencyTypes =
    [
        ("HMS-kurs (årlig)",       "Pliktig HMS-opplæring for alle ansatte.",           "HMS",        true),
        ("Førstehjelp",            "Kurs i førstehjelp og livredning.",                  "HMS",        true),
        ("Førerkort klasse B",     "Vanlig personbilførerkort.",                        "Sertifikat", true),
        ("Førerkort klasse C",     "Tung lastebilførerkort.",                           "Sertifikat", true),
        ("Prosjektledelse PRINCE2","Sertifisering i prosjektmetodikken PRINCE2.",       "Kurs",       true),
        ("Agile Scrum Master",     "Scrum Master-sertifisering.",                        "Kurs",       true),
    ];

    // Competencies: (UserEmail, CompetencyTypeName, IssuedDateOffset, ExpiryDateOffset, CertificateNumber)
    // Offsets are relative to today (DateTime.UtcNow.Date)
    private static readonly (string UserEmail, string CompetencyTypeName, int IssuedDateOffsetDays, int? ExpiryDateOffsetDays, string? CertificateNumber)[] Competencies =
    [
        // Alle ansatte: HMS-kurs (årlig)
        ("lars.hansen@compvault.no",  "HMS-kurs (årlig)",       -180,  180, "HMS-2025-001"),
        ("ingrid.berg@compvault.no",  "HMS-kurs (årlig)",       -180,   45, "HMS-2025-002"),
        ("tobias.lie@compvault.no",   "HMS-kurs (årlig)",       -180,  200, "HMS-2025-003"),
        ("sofie.dahl@compvault.no",   "HMS-kurs (årlig)",       -400,  -30, "HMS-2024-001"),

        // IT-folk: Førerkort
        ("lars.hansen@compvault.no",  "Førerkort klasse B",    -730, 1095, null),
        ("ingrid.berg@compvault.no",  "Førerkort klasse B",    -730,   60, null),
        ("tobias.lie@compvault.no",   "Førerkort klasse C",    -365, 1460, "C-20240101"),

        // Spesifikke kompetanser
        ("lars.hansen@compvault.no",  "Agile Scrum Master",     -90,   365, "SM-2025-001"),
        ("tobias.lie@compvault.no",   "Prosjektledelse PRINCE2", -60, 730, "PR2-2025-001"),
        ("ola.nordmann@compvault.no", "Førstehjelp",           -180,   75, "FH-2025-001"),
    ];

    // DocumentTypes: (Name, Slug, Description, TargetMode)
    private static readonly (string Name, string Slug, string? Description, DocumentTargetMode TargetMode)[] DocumentTypesData =
    [
        ("HMS Dokumenter", "hms-documents",
            "Helse-, miljø- og sikkerhetsdokumenter. Gjelder for avdelinger.",
            DocumentTargetMode.Department),
        ("Stillingsinstrukser", "position-instructions",
            "Arbeidsinstrukser og stillingsbeskrivelser. Gjelder for jobbtitler.",
            DocumentTargetMode.JobTitle),
        ("Kursmateriell", "course-materials",
            "Kursmateriell og opplæringsdokumenter. Tilgjengelig for alle.",
            DocumentTargetMode.None),
    ];

    // DocumentTypeCategories: (DocumentTypeSlug, Name, Slug)
    private static readonly (string DocumentTypeSlug, string Name, string Slug)[] DocumentTypeCategoriesData =
    [
        // HMS
        ("hms-documents", "Nødsprosedyrer", "emergency-procedure"),
        ("hms-documents", "Sikkerhetsinstrukser", "safety-instruction"),
        ("hms-documents", "Risikovurderinger", "risk-assessment"),
        ("hms-documents", "Sjekklister", "checklist"),
        ("hms-documents", "Opplæringsmateriell", "training-material"),
        ("hms-documents", "Retningslinjer", "policy"),
        ("hms-documents", "Rapporter", "report"),
        // Stillingsinstrukser
        ("position-instructions", "Stillingsbeskrivelser", "job-description"),
        ("position-instructions", "Arbeidsinstrukser", "work-instructions"),
        ("position-instructions", "Ansvarsområder", "responsibilities"),
        ("position-instructions", "Prosedyrer", "procedures"),
        ("position-instructions", "Retningslinjer", "guidelines"),
        ("position-instructions", "Kompetansekrav", "competency-requirements"),
    ];

    // Documents: (DocumentTypeSlug, CategorySlug, Title, RequiresSignature, TargetDepartmentName, TargetJobTitleName)
    private static readonly (string DocumentTypeSlug, string? CategorySlug, string Title, bool RequiresSignature, string? TargetDepartmentName, string? TargetJobTitleName)[] DocumentsData =
    [
        // HMS documents with department targeting
        ("hms-documents", "emergency-procedure", "Brannverninstruks", true, "IT", null),
        ("hms-documents", "safety-instruction", "Verneutstyr - hørselvern", true, "Utvikling", null),
        ("hms-documents", "risk-assessment", "Risikovurdering - hjemmekontor", true, "Drift", null),
        ("hms-documents", "checklist", "Ukentlig HMS-sjekkliste", false, "HR", null),
        ("hms-documents", "policy", "HMS-policy", true, "Ledelse", null),

        // Position instructions with job title targeting
        ("position-instructions", "job-description", "Systemutvikler - stillingsbeskrivelse", false, null, "Systemutvikler"),
        ("position-instructions", "responsibilities", "IT-leder - ansvarsområder", false, null, "IT-leder"),

        // Course materials (no targeting, available to all)
        ("course-materials", null, "Onboarding-guide for nye ansatte", false, null, null),
        ("course-materials", null, "CompVault presentasjon", false, null, null),
    ];

    /// <summary>
    /// Kjør seed. Oppretter roller, brukere, avdelinger og kompetanser dersom de ikke allerede finnes.
    /// </summary>
    public static async Task SeedAsync(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        AppDbContext dbContext,
        ILogger logger)
    {
        logger.LogInformation("[DatabaseSeeder] Starter seeding av testdata...");

        await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            await SeedRolesAsync(roleManager, logger);
            await SeedPermissionsAsync(dbContext, logger);
            await SeedRolePermissionsAsync(roleManager, dbContext, logger);
            await SeedUsersAsync(userManager, logger);
            await SeedDepartmentsAsync(dbContext, logger);
            await SeedUserDepartmentsAsync(userManager, dbContext, logger);
            await SeedJobTitlesAsync(dbContext, logger);
            await SeedUserJobTitlesAsync(userManager, dbContext, logger);
            await SeedCompetencyTypesAsync(dbContext, logger);
            await SeedCompetenciesAsync(dbContext, logger);
            await SeedDocumentTypesAsync(dbContext, logger);
            await SeedDocumentTypeCategoriesAsync(dbContext, logger);
            await SeedDocumentsAsync(dbContext, logger);

            await transaction.CommitAsync();
            logger.LogInformation("[DatabaseSeeder] Seeding fullført.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex, "[DatabaseSeeder] Feil under seeding - transaksjon rullet tilbake");
            throw;
        }
    }

    private static async Task SeedRolesAsync(RoleManager<ApplicationRole> roleManager, ILogger logger)
    {
        foreach ((string name, string description) in Roles)
        {
            if (await roleManager.RoleExistsAsync(name))
                continue;

            ApplicationRole role = new()
            {
                Name = name,
                Description = description,
                IsSystem = true,
                CreatedAt = DateTime.UtcNow,
            };

            IdentityResult result = await roleManager.CreateAsync(role);
            if (result.Succeeded)
                logger.LogInformation("[DatabaseSeeder] Rolle opprettet: {Role}", name);
            else
                logger.LogWarning("[DatabaseSeeder] Feil ved opprettelse av rolle {Role}: {Errors}",
                    name, string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }

    private static async Task SeedUsersAsync(UserManager<ApplicationUser> userManager, ILogger logger)
    {
        foreach ((string firstName, string lastName, string email, string[] roles) in Users)
        {
            if (await userManager.FindByEmailAsync(email) is not null)
                continue;

            ApplicationUser user = new()
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = firstName,
                LastName = lastName,
                EmploymentType = EmploymentType.Permanent,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            };

            IdentityResult createResult = await userManager.CreateAsync(user, DefaultPassword);
            if (!createResult.Succeeded)
            {
                logger.LogWarning("[DatabaseSeeder] Feil ved opprettelse av bruker {Email}: {Errors}",
                    email, string.Join(", ", createResult.Errors.Select(e => e.Description)));
                continue;
            }

            IdentityResult roleResult = await userManager.AddToRolesAsync(user, roles);
            if (roleResult.Succeeded)
                logger.LogInformation("[DatabaseSeeder] Bruker opprettet: {Email} ({Roles})",
                    email, string.Join(", ", roles));
            else
                logger.LogWarning("[DatabaseSeeder] Feil ved tildeling av roller til {Email}: {Errors}",
                    email, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
        }
    }

    private static async Task SeedDepartmentsAsync(AppDbContext dbContext, ILogger logger)
    {
        // First pass: create all top-level departments
        foreach ((string name, string description, string? parentName) in Departments)
        {
            if (parentName is not null)
                continue; // Will be created in second pass

            bool exists = await dbContext.Departments.AnyAsync(d => d.Name == name);
            if (exists)
                continue;

            ApplicationUser? admin = await dbContext.Users.OrderBy(u => u.CreatedAt).FirstOrDefaultAsync();

            Department dept = new()
            {
                Name = name,
                Description = description,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                CreatedById = admin?.Id,
            };

            dbContext.Departments.Add(dept);
            await dbContext.SaveChangesAsync();
            logger.LogInformation("[DatabaseSeeder] Avdeling opprettet: {Name}", name);
        }

        // Second pass: create child departments
        foreach ((string name, string description, string? parentName) in Departments)
        {
            if (parentName is null)
                continue;

            bool exists = await dbContext.Departments.AnyAsync(d => d.Name == name);
            if (exists)
                continue;

            Department? parent = await dbContext.Departments.FirstOrDefaultAsync(d => d.Name == parentName);
            if (parent is null)
            {
                logger.LogWarning("[DatabaseSeeder] Kunne ikke finne parent-avdeling {Parent} for {Name}", parentName, name);
                continue;
            }

            ApplicationUser? admin = await dbContext.Users.OrderBy(u => u.CreatedAt).FirstOrDefaultAsync();

            Department dept = new()
            {
                Name = name,
                Description = description,
                ParentDepartmentId = parent.Id,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                CreatedById = admin?.Id,
            };

            dbContext.Departments.Add(dept);
            await dbContext.SaveChangesAsync();
            logger.LogInformation("[DatabaseSeeder] Avdeling opprettet: {Name} (under {Parent})", name, parentName);
        }
    }

    private static async Task SeedUserDepartmentsAsync(
        UserManager<ApplicationUser> userManager,
        AppDbContext dbContext,
        ILogger logger)
    {
        foreach ((string email, string deptName) in UserDepartments)
        {
            ApplicationUser? user = await userManager.FindByEmailAsync(email);
            if (user is null)
            {
                logger.LogWarning("[DatabaseSeeder] Bruker ikke funnet for avdelingskobling: {Email}", email);
                continue;
            }

            Department? dept = await dbContext.Departments.FirstOrDefaultAsync(d => d.Name == deptName);
            if (dept is null)
            {
                logger.LogWarning("[DatabaseSeeder] Avdeling ikke funnet for kobling: {Dept}", deptName);
                continue;
            }

            if (user.DepartmentId == dept.Id)
                continue; // Already linked

            user.DepartmentId = dept.Id;
            IdentityResult result = await userManager.UpdateAsync(user);
            if (result.Succeeded)
                logger.LogInformation("[DatabaseSeeder] Bruker {Email} koblet til avdeling {Dept}", email, deptName);
            else
                logger.LogWarning("[DatabaseSeeder] Feil ved kobling av {Email} til {Dept}: {Errors}",
                    email, deptName, string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }

    private static async Task SeedCompetencyTypesAsync(AppDbContext dbContext, ILogger logger)
    {
        foreach ((string name, string? description, string? category, bool requiresExpiration) in CompetencyTypes)
        {
            bool exists = await dbContext.CompetencyTypes.AnyAsync(ct => ct.Name == name);
            if (exists)
                continue;

            CompetencyType ct = new()
            {
                Name = name,
                Description = description,
                Category = category,
                RequiresExpiration = requiresExpiration,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
            };

            dbContext.CompetencyTypes.Add(ct);
            await dbContext.SaveChangesAsync();
            logger.LogInformation("[DatabaseSeeder] Kompetansetype opprettet: {Name}", name);
        }
    }

    private static async Task SeedJobTitlesAsync(AppDbContext dbContext, ILogger logger)
    {
        foreach (string name in JobTitlesData)
        {
            bool exists = await dbContext.JobTitles.AnyAsync(jt => jt.Name == name);
            if (exists)
                continue;

            JobTitle jobTitle = new()
            {
                Name = name,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
            };

            dbContext.JobTitles.Add(jobTitle);
            await dbContext.SaveChangesAsync();
            logger.LogInformation("[DatabaseSeeder] Stillingstittel opprettet: {Name}", name);
        }
    }

    private static async Task SeedUserJobTitlesAsync(
        UserManager<ApplicationUser> userManager,
        AppDbContext dbContext,
        ILogger logger)
    {
        foreach ((string email, string jobTitleName) in UserJobTitles)
        {
            ApplicationUser? user = await userManager.FindByEmailAsync(email);
            if (user is null)
            {
                logger.LogWarning("[DatabaseSeeder] Bruker ikke funnet for stillingstittel-kobling: {Email}", email);
                continue;
            }

            JobTitle? jobTitle = await dbContext.JobTitles.FirstOrDefaultAsync(jt => jt.Name == jobTitleName);
            if (jobTitle is null)
            {
                logger.LogWarning("[DatabaseSeeder] Stillingstittel ikke funnet for kobling: {JobTitleName}", jobTitleName);
                continue;
            }

            if (user.JobTitleId == jobTitle.Id)
                continue; // Allerede koblet

            user.JobTitleId = jobTitle.Id;
            IdentityResult result = await userManager.UpdateAsync(user);
            if (result.Succeeded)
                logger.LogInformation("[DatabaseSeeder] Bruker {Email} koblet til stillingstittel {JobTitleName}", email, jobTitleName);
            else
                logger.LogWarning("[DatabaseSeeder] Feil ved kobling av {Email} til {JobTitleName}: {Errors}",
                    email, jobTitleName, string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }

    private static async Task SeedCompetenciesAsync(AppDbContext dbContext, ILogger logger)
    {
        DateTime today = DateTime.UtcNow.Date;

        foreach ((string email, string typeName, int issuedOffsetDays, int? expiryOffsetDays, string? certNumber) in Competencies)
        {
            ApplicationUser? user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user is null)
            {
                logger.LogWarning("[DatabaseSeeder] Bruker ikke funnet: {Email}", email);
                continue;
            }

            CompetencyType? ct = await dbContext.CompetencyTypes.FirstOrDefaultAsync(c => c.Name == typeName);
            if (ct is null)
            {
                logger.LogWarning("[DatabaseSeeder] Kompetansetype ikke funnet: {Type}", typeName);
                continue;
            }

            // Check if this competency already exists (user + type combination)
            bool exists = await dbContext.Competencies.AnyAsync(
                c => c.UserId == user.Id && c.CompetencyTypeId == ct.Id);
            if (exists)
                continue;

            DateTime issuedDate = today.AddDays(issuedOffsetDays);
            DateTime? expiryDate = expiryOffsetDays.HasValue
                ? today.AddDays(expiryOffsetDays.Value)
                : null;

            CompetencyStatus status = CompetencyStatusCalculator.Calculate(expiryDate);

            Competency competency = new()
            {
                UserId = user.Id,
                CompetencyTypeId = ct.Id,
                Status = status,
                IssuedDate = issuedDate,
                ExpiryDate = expiryDate,
                CertificateNumber = certNumber,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
            };

            dbContext.Competencies.Add(competency);
            await dbContext.SaveChangesAsync();
            logger.LogInformation("[DatabaseSeeder] Kompetanse opprettet: {User} - {Type} ({Status})",
                email, typeName, status);
        }
    }

    private static async Task SeedPermissionsAsync(AppDbContext dbContext, ILogger logger)
    {
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
            (Permissions.JobTitlesRead, "Se stillingstitler", "JobTitles"),
            (Permissions.JobTitlesWrite, "Opprett/endre stillingstitler", "JobTitles"),
            (Permissions.JobTitlesDelete, "Slett stillingstitler", "JobTitles"),
        ];

        int addedCount = 0;
        foreach ((string name, string description, string category) in permissions)
        {
            bool exists = await dbContext.Permissions.AnyAsync(p => p.Name == name);
            if (exists)
                continue;

            Permission permission = new()
            {
                Name = name,
                Description = description,
                Category = category,
            };

            dbContext.Permissions.Add(permission);
            addedCount++;
        }

        if (addedCount > 0)
        {
            await dbContext.SaveChangesAsync();
            logger.LogInformation("[DatabaseSeeder] {Count} permissions opprettet", addedCount);
        }
    }

    private static async Task SeedRolePermissionsAsync(
        RoleManager<ApplicationRole> roleManager,
        AppDbContext dbContext,
        ILogger logger)
    {
        ApplicationRole? adminRole = await roleManager.FindByNameAsync("Admin");
        ApplicationRole? employeeRole = await roleManager.FindByNameAsync("Employee");

        if (adminRole is null || employeeRole is null)
        {
            logger.LogWarning("[DatabaseSeeder] Kunne ikke finne Admin eller Employee rolle for permission-seeding");
            return;
        }

        List<Permission> allPermissions = await dbContext.Permissions.ToListAsync();

        // Admin: Alle permissions - check each individually
        int adminAddedCount = 0;
        foreach (Permission permission in allPermissions)
        {
            bool exists = await dbContext.RolePermissions.AnyAsync(
                rp => rp.RoleId == adminRole.Id && rp.PermissionId == permission.Id);
            if (exists)
                continue;

            dbContext.RolePermissions.Add(new RolePermission
            {
                RoleId = adminRole.Id,
                PermissionId = permission.Id,
                GrantedAt = DateTime.UtcNow,
            });
            adminAddedCount++;
        }

        if (adminAddedCount > 0)
        {
            await dbContext.SaveChangesAsync();
            logger.LogInformation("[DatabaseSeeder] Admin tildelt {Count} permissions", adminAddedCount);
        }

        // Employee: Kun read permissions for users, departments, competencies (ikke roles!)
        string[] employeePermissionNames =
        [
            Permissions.UsersRead,
            Permissions.DepartmentsRead,
            Permissions.CompetenciesRead,
            Permissions.DocumentTypesRead,
            Permissions.DocumentsRead,
            Permissions.DocumentsSign,
            Permissions.JobTitlesRead,
        ];

        var employeePermissions = allPermissions
            .Where(p => employeePermissionNames.Contains(p.Name))
            .ToList();

        int employeeAddedCount = 0;
        foreach (Permission permission in employeePermissions)
        {
            bool exists = await dbContext.RolePermissions.AnyAsync(
                rp => rp.RoleId == employeeRole.Id && rp.PermissionId == permission.Id);
            if (exists)
                continue;

            dbContext.RolePermissions.Add(new RolePermission
            {
                RoleId = employeeRole.Id,
                PermissionId = permission.Id,
                GrantedAt = DateTime.UtcNow,
            });
            employeeAddedCount++;
        }

        if (employeeAddedCount > 0)
        {
            await dbContext.SaveChangesAsync();
            logger.LogInformation("[DatabaseSeeder] Employee tildelt {Count} read permissions", employeeAddedCount);
        }
    }

    private static readonly string[] DefaultAllowedMimeTypes =
    [
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    ];

    private static async Task SeedDocumentTypesAsync(AppDbContext dbContext, ILogger logger)
    {
        foreach ((string name, string slug, string? description, DocumentTargetMode targetMode) in DocumentTypesData)
        {
            bool exists = await dbContext.DocumentTypes.AnyAsync(dt => dt.Slug == slug);
            if (exists)
                continue;

            ApplicationUser? admin = await dbContext.Users.OrderBy(u => u.CreatedAt).FirstOrDefaultAsync();

            var documentType = new DocumentType
            {
                Name = name,
                Slug = slug,
                Description = description,
                TargetMode = targetMode,
                StorageFolder = slug,
                AllowedMimeTypes = DefaultAllowedMimeTypes,
                MaxFileSizeBytes = 20 * 1024 * 1024,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedById = admin?.Id
            };

            dbContext.DocumentTypes.Add(documentType);
            await dbContext.SaveChangesAsync();
            logger.LogInformation("[DatabaseSeeder] Dokumenttype opprettet: {Name} ({Slug})", name, slug);
        }
    }

    private static async Task SeedDocumentTypeCategoriesAsync(AppDbContext dbContext, ILogger logger)
    {
        foreach ((string documentTypeSlug, string name, string slug) in DocumentTypeCategoriesData)
        {
            DocumentType? documentType = await dbContext.DocumentTypes
                .FirstOrDefaultAsync(dt => dt.Slug == documentTypeSlug);
            if (documentType is null)
            {
                logger.LogWarning("[DatabaseSeeder] Dokumenttype ikke funnet for kategori: {Slug}", documentTypeSlug);
                continue;
            }

            bool exists = await dbContext.DocumentTypeCategories
                .AnyAsync(c => c.DocumentTypeId == documentType.Id && c.Slug == slug);
            if (exists)
                continue;

            var category = new DocumentTypeCategory
            {
                DocumentTypeId = documentType.Id,
                Name = name,
                Slug = slug,
                IsActive = true
            };

            dbContext.DocumentTypeCategories.Add(category);
            await dbContext.SaveChangesAsync();
            logger.LogInformation("[DatabaseSeeder] Kategori opprettet: {Name} for {DocumentType}", name, documentTypeSlug);
        }
    }

    private static async Task SeedDocumentsAsync(AppDbContext dbContext, ILogger logger)
    {
        foreach ((string documentTypeSlug, string? categorySlug, string title, bool requiresSignature, string? targetDeptName, string? targetJobTitleName) in DocumentsData)
        {
            DocumentType? documentType = await dbContext.DocumentTypes
                .FirstOrDefaultAsync(dt => dt.Slug == documentTypeSlug);
            if (documentType is null)
            {
                logger.LogWarning("[DatabaseSeeder] Dokumenttype ikke funnet for dokument: {Slug}", documentTypeSlug);
                continue;
            }

            Guid? categoryId = null;
            if (categorySlug is not null)
            {
                DocumentTypeCategory? category = await dbContext.DocumentTypeCategories
                    .FirstOrDefaultAsync(c => c.DocumentTypeId == documentType.Id && c.Slug == categorySlug);
                categoryId = category?.Id;
            }

            Guid? targetDeptId = null;
            if (targetDeptName is not null)
            {
                Department? dept = await dbContext.Departments.FirstOrDefaultAsync(d => d.Name == targetDeptName);
                targetDeptId = dept?.Id;
            }

            Guid? targetJobTitleId = null;
            if (targetJobTitleName is not null)
            {
                JobTitle? jobTitle = await dbContext.JobTitles.FirstOrDefaultAsync(jt => jt.Name == targetJobTitleName);
                targetJobTitleId = jobTitle?.Id;
            }

            // Unngå duplikater ved seeding
            bool documentExists = await dbContext.Documents.AnyAsync(d =>
                d.Title == title &&
                d.DocumentTypeId == documentType.Id &&
                d.TargetDepartmentId == targetDeptId &&
                d.TargetJobTitleId == targetJobTitleId);
            if (documentExists)
                continue;

            ApplicationUser? admin = await dbContext.Users.OrderBy(u => u.CreatedAt).FirstOrDefaultAsync();
            if (admin is null)
            {
                logger.LogWarning("[DatabaseSeeder] Ingen admin funnet — dokument {Title} hoppes over.", title);
                continue;
            }

            var document = new Document
            {
                DocumentTypeId = documentType.Id,
                DocumentTypeCategoryId = categoryId,
                Title = title,
                RequiresSignature = requiresSignature,
                TargetDepartmentId = targetDeptId,
                TargetJobTitleId = targetJobTitleId,
                Version = 1,
                IsActive = true,
                UploadedBy = admin.Id,
                UploadedAt = DateTime.UtcNow,
            };

            dbContext.Documents.Add(document);
            await dbContext.SaveChangesAsync();
            logger.LogInformation("[DatabaseSeeder] Dokument opprettet: {Title}", title);
        }
    }
}