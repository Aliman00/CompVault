using CompVault.Backend.Features.Departments.Services;

namespace CompVault.Backend.Tests.Common;

/// <summary>
/// Hopper over hierarki-sjekk der vi ikke trenger det
/// </summary>
public sealed class BypassDepartmentScopeService : IDepartmentScopeService
{
    public bool HasBypass(string readAllPermission) => true;
    public IReadOnlyList<Guid> GetAllowedDepartmentIds(string? readSubPermission = null) => [];
    public bool IsAllowed(Guid departmentId, string readAllPermission, string? readSubPermission = null) => true;
}