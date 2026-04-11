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

    // --- Fase 4+ (legg til etterhvert som fasene implementeres) ---
    // Documents, Requirements, Equipment, Onboarding, ...
}