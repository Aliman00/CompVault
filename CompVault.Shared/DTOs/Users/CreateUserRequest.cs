using System.ComponentModel.DataAnnotations;
using CompVault.Shared.Enums;

namespace CompVault.Shared.DTOs.Users;

/// <summary>
/// Det som sendes inn for å opprette en ny bruker.
/// </summary>
public sealed class CreateUserRequest
{
    /// <summary>E-postadressen (brukes som brukernavn).</summary>
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    /// <summary>Fornavn.</summary>
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Etternavn.</summary>
    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    /// <summary>Stillingstittel (valgfritt).</summary>
    [MaxLength(150)]
    public string JobTitle { get; set; } = string.Empty;

    /// <summary>Ansettelsestype.</summary>
    [Required]
    public EmploymentType EmploymentType { get; set; } = EmploymentType.Permanent;

    /// <summary>Valgfri avdelings-ID.</summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>Valgfri leder-ID.</summary>
    public Guid? ManagerId { get; set; }

    /// <summary>Rollene som skal tildeles brukeren med en gang.</summary>
    public IList<string> Roles { get; set; } = [];
}
