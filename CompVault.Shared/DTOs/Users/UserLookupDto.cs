namespace CompVault.Shared.DTOs.Users;

/// <summary>
/// DTO for å vise brukere i en select-dropdown for å kunne utføre valg på denne brukerne
/// </summary>
public sealed class UserLookupDto
{   
    /// <summary> ID-til brukeren </summary>
    public Guid Id { get; set; }
    
    /// <summary> Fultnavn til brukeren </summary>
    public string FullName { get; set; } = string.Empty;
    
    /// <summary> Avdelingsnavn for å diffiransiere mellom brukere med likt navn </summary>
    public string? DepartmentName { get; set; }
    
    /// <summary> Stillingstittel for å diffiransiere mellom brukere med likt navn og eventuelt avdeling </summary>
    public string? JobTitleName { get; set; }
}