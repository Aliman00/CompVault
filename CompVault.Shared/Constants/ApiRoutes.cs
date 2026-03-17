namespace CompVault.Shared.Constants;

/// <summary>
/// API-rutene til frontend, backend og testing. Backend bruker kun den enkle stien
/// Frontend og testing bruker base for kontrollerens sti, sammen med endepunktet
/// </summary>
public static class ApiRoutes
{
    public static class Auth
    {
        private const string Base = "api/auth";

        public const string RequestOtp = "request-otp";
        public const string VerifyOtp = "verify-otp";
        public const string Refresh = "refresh";
        public const string Revoke = "revoke";

        public const string RequestOtpFull = $"{Base}/{RequestOtp}";
        public const string VerifyOtpFull = $"{Base}/{VerifyOtp}";
        public const string RefreshFull = $"{Base}/{Refresh}";
        public const string RevokeFull = $"{Base}/{Revoke}";
    }
}
