using System.Text.Json;

namespace CompVault.Shared.DTOs.Audit;

/// <summary>
/// DTO for én revisjonslogg-oppføring.
/// </summary>
public class AuditLogDto
{
    public Guid Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public string? UserEmail { get; set; }
    public JsonElement? Details { get; set; }
    public DateTime CreatedAt { get; set; }
}