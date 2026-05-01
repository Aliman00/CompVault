using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Infrastructure.Data;
using CompVault.Shared.Constants;

using Microsoft.EntityFrameworkCore;

namespace CompVault.Backend.SeedData.Seeders;

public static class PermissionSeeder
{
    public static async Task SeedAsync(AppDbContext dbContext, ILogger logger)
    {
        (string Name, string Description, string Category)[] permissions =
        [
            (Permissions.UsersRead, "Se brukere", "Users"),
            (Permissions.UsersWrite, "Opprett/endre brukere", "Users"),
            (Permissions.UsersDelete, "Slett brukere", "Users"),
            (Permissions.UsersAll, "Se brukere i alle avdelinger", "Users"),
            (Permissions.UsersReadSub, "Se brukere i underavdelinger", "Users"),

            (Permissions.RolesRead, "Se roller", "Roles"),
            (Permissions.RolesWrite, "Opprett/endre roller", "Roles"),
            (Permissions.RolesDelete, "Slett roller", "Roles"),

            (Permissions.DepartmentsRead, "Se avdelinger", "Departments"),
            (Permissions.DepartmentsWrite, "Opprett/endre avdelinger", "Departments"),
            (Permissions.DepartmentsDelete, "Slett avdelinger", "Departments"),
            (Permissions.DepartmentsAll, "Se alle avdelinger", "Departments"),
            (Permissions.DepartmentsReadSub, "Se underavdelinger", "Departments"),

            (Permissions.CompetenciesRead, "Se kompetanser", "Competencies"),
            (Permissions.CompetenciesWrite, "Opprett/endre kompetanser", "Competencies"),
            (Permissions.CompetenciesDelete, "Slett kompetanser", "Competencies"),
            (Permissions.CompetenciesAll, "Se kompetanser i alle avdelinger", "Competencies"),
            (Permissions.CompetenciesReadSub, "Se kompetanser i underavdelinger", "Competencies"),

            (Permissions.DocumentTypesRead, "Se dokumenttyper", "DocumentTypes"),
            (Permissions.DocumentTypesWrite, "Opprett/endre dokumenttyper", "DocumentTypes"),
            (Permissions.DocumentTypesDelete, "Slett dokumenttyper", "DocumentTypes"),

            (Permissions.DocumentsRead, "Se dokumenter", "Documents"),
            (Permissions.DocumentsWrite, "Opprett/endre dokumenter", "Documents"),
            (Permissions.DocumentsDelete, "Slett dokumenter", "Documents"),
            (Permissions.DocumentsSign, "Signere dokumenter", "Documents"),
            (Permissions.DocumentsAll, "Se dokumenter i alle avdelinger", "Documents"),
            (Permissions.DocumentsReadSub, "Se dokumenter i underavdelinger", "Documents"),

            (Permissions.JobTitlesRead, "Se stillingstitler", "JobTitles"),
            (Permissions.JobTitlesWrite, "Opprett/endre stillingstitler", "JobTitles"),
            (Permissions.JobTitlesDelete, "Slett stillingstitler", "JobTitles"),

            (Permissions.EquipmentRead, "Se utstyr", "Equipment"),
            (Permissions.EquipmentWrite, "Opprett/endre utstyr", "Equipment"),
            (Permissions.EquipmentDelete, "Slett utstyr", "Equipment"),
            (Permissions.EquipmentAll, "Se utstyr i alle avdelinger", "Equipment"),
            (Permissions.EquipmentReadSub, "Se utstyr i underavdelinger", "Equipment"),

            (Permissions.AdminAccess, "Se administratorpanel", "Admins"),
            (Permissions.AuditRead, "Tilgang til revisjonslogg", "Audit"),
        ];

        int addedCount = 0;
        foreach ((string name, string description, string category) in permissions)
        {
            bool exists = await dbContext.Permissions.AnyAsync(p => p.Name == name);
            if (exists)
                continue;

            dbContext.Permissions.Add(new Permission
            {
                Name = name,
                Description = description,
                Category = category,
            });
            addedCount++;
        }

        if (addedCount > 0)
        {
            await dbContext.SaveChangesAsync();
            logger.LogDebug("[Seeder] {Count} permissions opprettet", addedCount);
        }
    }
}