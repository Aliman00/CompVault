namespace CompVault.Shared.Constants;

/// <summary>
/// Alle tillatelsesstrenger i systemet. Backend bruker disse i autorisasjonspolicyer.
/// Frontend bruker dem til å vise/skjule UI-elementer basert på brukerens claims.
/// Nye tillatelser legges til her etterhvert som nye faser implementeres.
/// </summary>
public static class Permissions
{
    /// <summary>
    /// Claim type used for permission claims in JWT tokens.
    /// </summary>
    public const string ClaimType = "permission";

    // Users
    public const string UsersRead = "users:read";
    public const string UsersWrite = "users:write";
    public const string UsersDelete = "users:delete";

    // Roles
    public const string RolesRead = "roles:read";
    public const string RolesWrite = "roles:write";
    public const string RolesDelete = "roles:delete";

    // Departments
    public const string DepartmentsRead = "departments:read";
    public const string DepartmentsWrite = "departments:write";
    public const string DepartmentsDelete = "departments:delete";

    // Competencies
    public const string CompetenciesRead = "competencies:read";
    public const string CompetenciesWrite = "competencies:write";
    public const string CompetenciesDelete = "competencies:delete";

    // AdminAccess
    public const string AdminAccess = "admin:access";

    // Document Types (admin — opprette og administrere dokumenttyper)
    public const string DocumentTypesRead = "document_types:read";
    public const string DocumentTypesWrite = "document_types:write";
    public const string DocumentTypesDelete = "document_types:delete";

    // Documents (generiske — tilgang per dokumenttype styres av DocumentType-konfigurasjon)
    public const string DocumentsRead = "documents:read";
    public const string DocumentsWrite = "documents:write";
    public const string DocumentsDelete = "documents:delete";
    public const string DocumentsSign = "documents:sign";
    public const string DocumentsAllDepartments = "documents:all_departments";

    // Job Titles
    public const string JobTitlesRead = "job_titles:read";
    public const string JobTitlesWrite = "job_titles:write";
    public const string JobTitlesDelete = "job_titles:delete";

    // Equipment
    public const string EquipmentRead = "equipment:read";
    public const string EquipmentWrite = "equipment:write";
    public const string EquipmentDelete = "equipment:delete";

    // Audit
    public const string AuditRead = "audit:read";
}