namespace CompVault.Backend.Infrastructure.Configuration;

/// <summary>
/// CORS-innstillinger hentet fra appsettings.json. Bind automatisk til seksjonen "Cors"
/// </summary>
public class CorsSettings
{
    public const string SectionName = "Cors";
    public const string PolicyName = "Frontend";

    /// <summary>Tillatte origins — kommaseparert liste over frontend-URLer.</summary>
    public string AllowedOrigins { get; set; } = string.Empty;

    /// <summary>Returnerer origins som array til bruk i CORS-konfigurasjonen.</summary>
    public string[] GetOrigins() => AllowedOrigins
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}