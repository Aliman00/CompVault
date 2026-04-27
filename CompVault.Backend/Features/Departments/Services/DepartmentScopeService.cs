using System.Security.Claims;

using CompVault.Backend.Infrastructure.Repositories.Departments;
using CompVault.Shared.Constants;

namespace CompVault.Backend.Features.Departments.Services;

public sealed class DepartmentScopeService(
    IHttpContextAccessor http,
    IDepartmentRepository departmentRepository)
    : IDepartmentScopeService
{

    private readonly Lazy<Task<IReadOnlyList<Guid>>> _allowedIds;
    
    public bool HasBypass
    {
        get
        {
            ClaimsPrincipal? user = http.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
                return false;
            
            return user.HasClaim(Permissions.ClaimType, Permissions.UsersAll)
                   || user.HasClaim(Permissions.ClaimType, Permissions.DocumentsAll)
                   || user.HasClaim(Permissions.ClaimType, Permissions.DepartmentsAll)
                   || user.HasClaim(Permissions.ClaimType, Permissions.CompetenciesAll)
                   || user.HasClaim(Permissions.ClaimType, Permissions.EquipmentAll);
        }
    }

    public IReadOnlyList<Guid> GetAllowedDepartmentIds() => _allowedIds.Value.GetAwaiter().GetResult();

    public bool IsAllowed(Guid departmentId) => HasBypass || GetAllowedDepartmentIds().Contains(departmentId);
    
    private async Task<IReadOnlyList<Guid>> ResolveAllowedIdsAsync(CancellationToken ct)
    {
        ClaimsPrincipal? user = http.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            return [];

        string? deptIdStr = user.FindFirstValue("department_id");
        if (!Guid.TryParse(deptIdStr, out Guid departmentId))
            return [];

        return await BreadthFirstSearchAsync(departmentId, ct);
    }
    
    private async Task<IReadOnlyList<Guid>> BreadthFirstSearchAsync(Guid rootId, CancellationToken ct)
    {
        IReadOnlyList<Department> all = await _departmentRepository.GetAllWithHierarchyAsync(ct);

        var result = new List<Guid>();
        var queue = new Queue<Guid>();
        queue.Enqueue(rootId);

        while (queue.Count > 0)
        {
            Guid current = queue.Dequeue();
            result.Add(current);

            foreach (Department child in all.Where(d => d.ParentDepartmentId == current && d.IsActive))
                queue.Enqueue(child.Id);
        }

        return result;
    }

}