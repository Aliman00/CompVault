using CompVault.Shared.DTOs.Departments;
namespace CompVault.Frontend.Features.Departments.Components;

/// <summary>
/// Klasse for å sende en avdeling med underavdelinger videre til hierarki-treet
/// </summary>
public sealed class DepartmentTreeNode
{
    public DepartmentDto Department { get; init; } = null!;
    public List<DepartmentTreeNode> Children { get; init; } = [];
}