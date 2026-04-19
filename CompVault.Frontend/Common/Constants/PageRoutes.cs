namespace CompVault.Frontend.Common.Constants;

/// <summary>
/// Hardkodet URL-en til Pages for å enklere kunne navigere fra andre sider. /page i Pages må settes manuelt,
/// og hvis de endres så må vi endre her samtidig
/// </summary>
public static class PageRoutes
{
    public static class Auth
    {
        public const string LoginEmail = "/login-email";
        public const string LoginOtp = "/login-otp";
    }

    public static class Users
    {
        public const string List = "/users";
        public static string Detail(Guid id) => $"/users/{id}";
        public const string Create = "/users/create";
    }
    
    public static class JobTitles
    {
        public const string List = "/jobtitles";
        public static string Detail(Guid id) => $"/jobtitles/{id}";
        public const string Create = "/jobtitles/create";
    }
    
    public static class Departments
    {
        public const string List = "/departments";
        public static string Detail(Guid id) => $"/departments/{id}";
        public const string Create = "/departments/create";
    }
    
    public static class Roles
    {
        public const string List = "/roles";
        public static string Detail(Guid id) => $"/roles/{id}";
        public const string Create = "/roles/create";
    }

    public static class Competencies
    {
        public const string List = "/competencies";
        public static string Detail(Guid id) => $"/competencies/{id}";
        public const string Create = "/competencies/create";
    }
    
    public static class CompetencyTypes
    {
        public const string List = "/competency-types";
        public static string Detail(Guid id) => $"/competency-types/{id}";
        public const string Create = "/competency-types/create";
    }

    public static class Admin
    {
        public const string Dashboard = "/admin/dashboard";
    }

    public static class Errors
    {
        public const string NotFound = "/not-found";
        public const string NotAuthorized = "/not-authorized";
    }
    
    public static class Dev
    {
        public const string Panel = "/dev";
    }
}