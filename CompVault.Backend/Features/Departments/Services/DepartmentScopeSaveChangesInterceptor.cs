using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Shared.Constants;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
namespace CompVault.Backend.Features.Departments.Services;

public sealed class DepartmentScopeSaveChangesInterceptor(IServiceProvider serviceProvider) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken ct = default)
    {
        // Sikrer at vi har en aktive kontekst - skal alltid være der normalt
        if (eventData.Context == null)
            return base.SavingChangesAsync(eventData, result, ct);

        IDepartmentScopeService? departmentScopeService = serviceProvider.GetService<IDepartmentScopeService>();
        // Ingen scope så kan det bety at vi er i en kontekst uten en HTTP-forespørsel som eks migrasjon eller seeding
        if (departmentScopeService == null)
            return base.SavingChangesAsync(eventData, result, ct);

        IHttpContextAccessor? http = serviceProvider.GetService<IHttpContextAccessor>();
        if (http?.HttpContext?.User.Identity?.IsAuthenticated != true)
            return base.SavingChangesAsync(eventData, result, ct);

        foreach (EntityEntry<ApplicationUser> entry in eventData.Context.ChangeTracker
                     .Entries<ApplicationUser>()
                     .Where(e => e.State is EntityState.Added or EntityState.Modified))
        {
            if (!departmentScopeService.IsAllowed(entry.Entity.DepartmentId, Permissions.UsersAll,
                    Permissions.UsersReadSub))
                throw new UnauthorizedAccessException(
                    "Du har ikke tilattelse til denne operasjonen.");
        }

        return base.SavingChangesAsync(eventData, result, ct);
    }
}