namespace CompVault.Tests.Common;

/// <summary>
/// Manuelt overstyrer appsettings.json sine verdier for testing
/// </summary>
public static class TestConfiguration
{
    public static readonly Dictionary<string, string?> Default = new()
    {
        ["JwtSettings:Secret"]   = "test-secret-som-er-lang-nok-til-jwt-validering-32tegn",
        ["JwtSettings:Issuer"]   = "CompVault",
        ["JwtSettings:Audience"] = "CompVault.Clients",
        ["JwtSettings:AccessTokenMinutes"]  = "15",
        ["JwtSettings:RefreshTokenDays"]    = "1",
            
        ["Email:ApiKey"]      = "test-api-key",
        ["Email:FromAddress"] = "test@example.com",
            
        ["Otp:MaxFailedAttempts"]           = "3",
        ["Otp:ExpirationMinutes"]           = "10",
        ["Otp:MinResponseTimeRequestOtpMs"] = "0",
        ["Otp:MinResponseTimeVerifyOtpMs"]  = "0",
            
        ["ConnectionStrings:Default"] = "ignored"
    };
}