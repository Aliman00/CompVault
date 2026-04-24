namespace CompVault.Frontend.Features.Audit.Constants;

/// <summary>
/// Lister for å gjøre det nkelere å kunne fitlrere etter action og entitetstype i Auditlog
/// </summary>
public static class AuditConstants
{
    public static readonly IReadOnlyList<string> AllActions =
    [
        "application_user.create", "application_user.update", "application_user.delete",
        "application_role.create", "application_role.update", "application_role.delete",
        "department.create", "department.update", "department.delete",
        "job_title.create", "job_title.update", "job_title.delete",
        "competency.create", "competency.update", "competency.delete", "competency.revoke",
        "competency_type.create", "competency_type.update", "competency_type.delete",
        "document.create", "document.update", "document.delete", "document.signature_removed",
        "document_type.create", "document_type.update", "document_type.delete",
        "document_type_category.create", "document_type_category.update", "document_type_category.delete",
        "document_signature.create", "document_signature.delete",
        "equipment_category.create", "equipment_category.update", "equipment_category.delete",
        "equipment_item.create", "equipment_item.update", "equipment_item.delete",
        "equipment_issuance.create", "equipment_issuance.update", "equipment_issuance.delete",
    ];
    
    public static readonly IReadOnlyList<string> AllEntityTypes =
    [
        "ApplicationUser", "ApplicationRole", "Department", "JobTitle",
        "Competency", "CompetencyType",
        "Document", "DocumentType", "DocumentTypeCategory", "DocumentSignature",
        "EquipmentCategory", "EquipmentItem", "EquipmentIssuance",
        "Permission",
    ];
}