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

    public static class Admin
    {
        public const string Dashboard = "/admin/dashboard";
        public const string Users = "/admin/users";
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