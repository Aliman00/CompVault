namespace CompVault.Frontend.Common.Constants;

/// <summary>
/// Setter API routes til konstante verdier for å sikre at det skrives korrekt.
/// Mest verdi for frontend, man kan vi eventuelt integerere denne med backend og flytte til Shared?
/// </summary>
public static class ApiRoutes
{
    public static class Auth
    {
        private const string Base       = "api/auth";
        public const string RequestOtp = $"{Base}/request-otp";
        public const string VerifyOtp  = $"{Base}/verify-otp";
        public const string Refresh    = $"{Base}/refresh";
        public const string Revoke     = $"{Base}/revoke";
    }
}