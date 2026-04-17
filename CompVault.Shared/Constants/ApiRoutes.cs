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

    public static class User
    {
        public const string Base = "api/users";
        public static string ById(Guid id) => $"{Base}/{id}";
    }

    public static class Department
    {
        public const string Base = "api/departments";
        public static string ById(Guid id) => $"{Base}/{id}";
    }
    
    public static class Role
    {
        public const string Base = "api/roles";
        public static string ById(Guid id) => $"{Base}/{id}";
        public static string Permissions(Guid id) => $"{Base}/{id}/permissions";
        public const string AllPermissions = $"{Base}/permissions";
    }
    
    public static class Competencies
    {
        public const string Base = "api/competencies";
        public static string ById(Guid id) => $"{Base}/{id}";
        public const string Expiring = $"{Base}/expiring";
    }
    
    public static class CompetencyTypes
    {
        public const string Base = "api/competencytypes";
        public static string ById(Guid id) => $"{Base}/{id}";
    }
    
    public static class JobTitle
    {
        public const string Base = "api/jobtitles";
        public static string ById(Guid id) => $"{Base}/{id}";
    }
}