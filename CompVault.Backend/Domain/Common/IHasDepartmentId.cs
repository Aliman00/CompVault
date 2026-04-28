namespace CompVault.Backend.Domain.Common;

/// <summary>
/// Markørinterface for å finne entiteter som skal valideres om brukeren har tilattelse ved skriveoperasjoner
/// </summary>
public interface IHasDepartmentId
{
    Guid DepartmentId { get; }
    string GetReadAllPermission();
    string GetReadSubPermission();
}