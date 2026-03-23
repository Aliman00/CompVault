namespace CompVault.Tests.Common.Constants;

/// <summary>
/// Konstanter som sikrer at vi bruker riktig verdier når vi tester
/// </summary>
public static class TestConstants
{
    public static class Users
    {
        // Email for aktiv og inaktiv bruker
        public const string DefaultEmailForActiveUser = "test@compvault.no";
        public const string DefaultEmailForInactiveUser = "donotreply@workowl.no";

        // ID-ene til brukerne
        public static readonly Guid ActiveUserId = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
        public static readonly Guid InactiveUserId = Guid.Parse("b2c3d4e5-f6a7-8901-bcde-f12345678901");
    }

    public static class Otp
    {
        public const string PlainTextOtpCode = "476859";
    }

    public static class RefreshTokens
    {
        public const string Token = "test-token";
    }

    public static class Roles
    {
        public const string Default = "Employee";
        public const string Admin = "Admin";
    }
}
