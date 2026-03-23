namespace CompVault.Backend.Infrastructure.Email.Config;

/// <summary>
/// Email-innstillinger hentet fra appsettings.json. Bind automatisk til seksjonen "Email".
/// </summary>
public sealed class EmailSettings
{
    public const string SectionName = "Email";
    public string ApiKey { get; init; } = string.Empty;
    public string FromAddress { get; init; } = string.Empty;
}
