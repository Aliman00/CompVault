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
        public const string UserList = "/users";
        public static string UserDetail(Guid id) => $"/users/{id}";
        public const string UserCreate = "/users/create";
    }
    
    public static class Departments
    {
        public const string DepartmentList = "/departments";
        public static string DepartmentDetail(Guid id) => $"/departments/{id}";
        public const string DepartmentCreate = "/departments/create";
    }
    
    public static class Roles
    {
        public const string RoleList = "/roles";
        public static string RoleDetail(Guid id) => $"/roles/{id}";
        public const string RoleCreate = "/roles/create";
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