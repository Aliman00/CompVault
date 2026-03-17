namespace CompVault.Frontend.Common.Configuration;

/// <summary>
/// Klasse for BackendApiSettings for å konfiguere HttpClient mot CompVault.Backend
/// </summary>
public sealed class BackendApiSettings
{
    public const string SectionName = "BackendApi";
    public const string ClientName = "BackendApi";
    public string BaseUrl { get; init; } = string.Empty;
}
