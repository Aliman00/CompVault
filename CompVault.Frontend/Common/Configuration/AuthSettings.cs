namespace CompVault.Frontend.Common.Configuration;

public sealed class AuthSettings
{
    public const string SectionName = "Auth";
    
    /// <summary>
    /// Levetid på auth-cookie i nettleseren
    /// Bør matche JwtSettings:RefreshTokenDays i backend
    /// </summary>
    public int CookieExpireDays { get; init; } = 7;
}