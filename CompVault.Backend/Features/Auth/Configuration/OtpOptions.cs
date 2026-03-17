namespace CompVault.Backend.Features.Auth.Configuration;

/// <summary>
/// Otp-innstillinger hentet fra appsettings.json. Bind automatisk til seksjonen "Otp"
/// </summary>
public sealed class OtpOptions
{
    /// <summary>
    /// Seksjonennavnet fra appsettings
    /// </summary>
    public const string SectionName = "Otp";

    /// <summary>
    /// Antall feilede forsøk pr kode
    /// </summary>
    public int MaxFailedAttempts { get; init; } = 3;

    /// <summary>
    /// Hvor lenge en kode er gyldig i minutter
    /// </summary>
    public int ExpirationMinutes { get; init; } = 10;

    /// <summary>
    /// Hvor lenge vi delayer RequestOtp i millisekunder
    /// </summary>
    public int MinResponseTimeRequestOtpMs { get; set; } = 500;

    /// <summary>
    /// Hvor lenge vi delayer VerifyOtp i millisekunder
    /// </summary>
    public int MinResponseTimeVerifyOtpMs { get; set; } = 500;
}