namespace CompVault.Frontend.Common.Constants;

/// <summary>
/// Hardkodet URL-en til Pages for å enklere kunne navigere fra andre sider. /page i Pages må settes manuelt,
/// og hvis de endres så må vi endre her samtidig
/// </summary>
public static class PageRoutes
{
    // =============================== AUTH =============================== 
    public static class Auth
    {
        public const string LoginEmail = "/";
        public const string LoginOtp = "/login-otp";
    }

    // =============================== EMPLOYEES =============================== 
    public static class Dashboard
    {
        public const string Home = "/dashboard";
    }

    public static class UserCompetencies
    {
        public const string List = "/my-competencies";
        public static string Detail(Guid id) => $"/my-competencies/{id}";
    }

    public static class UserEquipment
    {
        public const string Overview = "/my-equipment";
        public static string List(Guid categoryId) => $"/my-equipment/{categoryId}";
        public static string Detail(Guid id) => $"/my-equipment/detail/{id}";
    }

    public static class UserDocuments
    {
        public const string Overview = "/my-documents";
        public static string List(string slug) => $"/my-documents/{slug}";
        public static string Detail(string slug, Guid id) => $"/my-documents/{slug}/{id}";
    }

    // =============================== ADMIN =============================== 

    public static class Users
    {
        public const string List = "/users";
        public static string Detail(Guid id) => $"/users/{id}";
        public const string Create = "/users/create";
        public const string MyProfile = "/profile";
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
        public static string CreateForUser(Guid userId) => $"/competencies/create?userId={userId}";
    }

    public static class CompetencyTypes
    {
        public const string List = "/competency-types";
        public static string Detail(Guid id) => $"/competency-types/{id}";
        public const string Create = "/competency-types/create";
    }

    public static class Documents
    {
        public const string Overview = "/documents";
        public static string List(string slug, string typeName) => $"/documents/{slug}/{typeName}";
        public static string Detail(string slug, Guid id) => $"/documents/{slug}/{id}";
        public static string Create(string slug) => $"/documents/{slug}/create";
    }

    public static class DocumentTypes
    {
        public const string List = "/document-types";
        public static string Detail(string slug) => $"/document-types/{slug}";
        public const string Create = "/document-types/create";
    }

    public static class EquipmentCategories
    {
        public const string List = "/equipment-categories";
        public static string Detail(Guid id) => $"/equipment-categories/{id}";
        public const string Create = "/equipment-categories/create";
    }

    public static class EquipmentItems
    {
        public static string ListByCategory(Guid categoryId) => $"/equipment-items?categoryId={categoryId}";
        public static string Detail(Guid id) => $"/equipment-items/{id}";
        public const string Create = "/equipment-items/create";
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

    public static class Audit
    {
        public const string List = "/audit-log";
    }

    public static class Dev
    {
        public const string Panel = "/dev";
    }
}