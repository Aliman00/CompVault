using System.Text.Json;

using CompVault.Backend.Domain.Entities.Audit;
using CompVault.Shared.DTOs.Audit;

namespace CompVault.Backend.Features.Audit;

/// <summary>
/// Mapper mellom AuditLog-entitet og AuditLogDto.
/// </summary>
public static class AuditLogMapper
{
    /// <summary>
    /// Konverterer en AuditLog-entitet til en AuditLogDto.
    /// Deserialiserer Details JSON til et objekt for fleksibel visning.
    /// </summary>
    public static AuditLogDto ToDto(AuditLog auditLog)
    {
        JsonElement? details = null;
        if (!string.IsNullOrEmpty(auditLog.Details))
        {
            try
            {
                details = JsonSerializer.Deserialize<JsonElement>(auditLog.Details);
            }
            catch
            {
                // Details er alltid skrevet av vår egen kode — parse-feil ignoreres stille
            }
        }

        return new AuditLogDto
        {
            Id = auditLog.Id,
            Action = auditLog.Action,
            EntityType = auditLog.EntityType,
            EntityId = auditLog.EntityId,
            UserId = auditLog.UserId,
            UserName = auditLog.UserName,
            UserEmail = auditLog.UserEmail,
            Details = details,
            CreatedAt = auditLog.CreatedAt,
        };
    }
}