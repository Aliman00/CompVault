namespace CompVault.Shared.Constants;

public class ApiRoutes
{
    public static class Auth
    {
        private const string Base = "api/auth";
        
        public const string RequestOtp = $"{Base}/request-otp";
        public const string VerifyOtp  = $"{Base}/verify-otp";
        public const string Refresh    = $"{Base}/refresh";
        public const string Revoke     = $"{Base}/revoke";
    }
}