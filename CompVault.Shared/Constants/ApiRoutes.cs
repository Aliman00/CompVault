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

    public static class DocumentTypes
    {
        public const string Base = "api/document-types";
        public static string BySlug(string slug) => $"{Base}/{slug}";
    }

    public static class DocumentTypeCategories // Base brukes ikke her siden det ligger inne i DocumentType, bruker All
    {
        private static string Base(string slug) => $"api/document-types/{slug}/categories";
        public static string All(string slug) => Base(slug);
        public static string ById(string slug, Guid categoryId) => $"{Base(slug)}/{categoryId}";
    }

    public static class Documents
    {
        // ================= Dokumenter =================
        public static string Base(string slug) => $"api/documents/{slug}";
        public static string ById(string slug, Guid id) => $"api/documents/{slug}/{id}";
        public static string UploadVersion(string slug, Guid id) => $"api/documents/{slug}/{id}/upload";
        
        // ================= Signaturer =================
        public static string Signatures(string slug, Guid id) => $"api/documents/{slug}/{id}/signatures";
        public static string Sign(string slug, Guid id) => $"api/documents/{slug}/{id}/sign";
        public const string MySigned = "api/documents/my/signed";
        public const string MyPending = "api/documents/my/pending";
        
        // ================= Nedlastning =================
        public static string Download(string slug, Guid id) => $"api/documents/{slug}/{id}/download";
    }
    
    public static class EquipmentCategories
    {
        public const string Base = "api/equipment-categories";
        public static string ById(Guid id) => $"{Base}/{id}";
    }

    public static class EquipmentItems
    {
        public const string Base = "api/equipment-items";
        public static string ById(Guid id) => $"{Base}/{id}";
        public static string ByCategory(Guid categoryId) => $"{Base}/by-category/{categoryId}";
    }

    public static class EquipmentIssuances
    {
        public const string Base = "api/equipment-issuances";
        public static string ById(Guid id) => $"{Base}/{id}";
        public static string ByUser(Guid userId) => $"{Base}/by-user/{userId}";
        public static string ByItem(Guid itemId) => $"{Base}/by-item/{itemId}";
    }

    public static class Audit
    {
        public const string Base = "api/audit-log";
    }
}