using System.Security.Claims;
using System.Text;
using System.Text.Json;

using CompVault.Backend.Domain.Entities.Audit;
using CompVault.Backend.Domain.Entities.Documents;
using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Features.Audit.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CompVault.Backend.Infrastructure.Data.Interceptors;

/// <summary>
/// EF Core SaveChangesInterceptor som automatisk fanger alle vesentlige endringer
/// og oppretter AuditLog-entries i samme transaksjon.
/// </summary>
public sealed class AuditSaveChangesInterceptor(IServiceProvider serviceProvider) : SaveChangesInterceptor
{
    private static readonly HashSet<string> IgnoredEntities = new(StringComparer.OrdinalIgnoreCase)
    {
        "OtpCode",
        "RefreshToken",
        "AuditLog",
        "DocumentDepartment",
        "DocumentJobTitle",
        "DocumentVersion",
        "RolePermission",
    };

    // Egenskaper som aldri skal inkluderes i changed_fields
    private static readonly HashSet<string> SkippedProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "Id",
        "CreatedAt",
    };

    /// <summary>
    /// Interceptor som kjører før SaveChangesAsync.
    /// </summary> <remarks>
    /// Går gjennom alle endrede entiteter i ChangeTracker, og for hver som er Added, Modified eller Deleted, oppretter en tilsvarende AuditLog-entry.
    /// For Modified-entries, fanger den kun endrede felter og deres gamle/ny verdier, og inkluderer også optional reason/action-override fra IAuditContext.
    /// For Deleted-entries, gjør den en spesiell håndtering for DocumentSignature for å fange relevant info før den forsvinner.
    /// Alle AuditLog-entries legges til i samme DbContext og dermed samme transaksjon, så de vil rulles tilbake hvis hovedoperasjonen feiler.
    /// </remarks>
    /// <param name="eventData">EventData fra EF Core</param>
    /// <param name="result">Resultat fra EF Core</param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns>InterceptionResult</returns>
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        DbContext? context = eventData.Context;
        if (context is null)
            return base.SavingChangesAsync(eventData, result, cancellationToken);

        IAuditContext? auditContext = serviceProvider.GetService<IAuditContext>();

        IHttpContextAccessor? httpContextAccessor = serviceProvider.GetService<IHttpContextAccessor>();
        (Guid? userId, string? userEmail, string? userName) = ResolveCurrentUser(httpContextAccessor);

        var auditEntries = new List<AuditLog>();
        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        foreach (EntityEntry? entry in entries)
        {
            string entityType = entry.Entity.GetType().Name;

            if (IgnoredEntities.Contains(entityType))
                continue;

            AuditLog? auditEntry = entry.State switch
            {
                EntityState.Added => BuildCreateEntry(entry, entityType, userId, userEmail, userName),
                EntityState.Modified => BuildModifiedEntry(entry, entityType, userId, userEmail, userName, auditContext),
                EntityState.Deleted => BuildDeletedEntry(entry, entityType, userId, userEmail, userName, context),
                _ => null
            };

            if (auditEntry is not null)
                auditEntries.Add(auditEntry);
        }

        if (auditEntries.Count > 0)
        {
            context.Set<AuditLog>().AddRange(auditEntries);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <inheritdoc />
    public override ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        IAuditContext? auditContext = serviceProvider.GetService<IAuditContext>();
        auditContext?.Clear();

        return base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>
    /// Bygger en AuditLog for en ny entitet.
    /// </summary>
    private static AuditLog BuildCreateEntry(
        EntityEntry entry, string entityType, Guid? userId, string? userEmail, string? userName)
    {
        var details = new Dictionary<string, object?>();

        foreach (PropertyEntry prop in entry.Properties)
        {
            if (ShouldSkipProperty(prop))
                continue;

            object? value = prop.CurrentValue;
            if (value is not null)
                details[prop.Metadata.Name] = value;
        }

        return new AuditLog
        {
            Action = $"{ToSnakeCase(entityType)}.create",
            EntityType = entityType,
            EntityId = GetEntityId(entry),
            UserId = userId,
            UserEmail = userEmail,
            UserName = userName,
            Details = details.Count > 0 ? JsonSerializer.Serialize(details) : null,
        };
    }

    /// <summary>
    /// Bygger en AuditLog for en endret entitet.
    /// </summary>
    private static AuditLog? BuildModifiedEntry(
        EntityEntry entry, string entityType, Guid? userId, string? userEmail, string? userName,
        IAuditContext? auditContext)
    {
        // Sjekk for soft-delete: DeletedAt endret fra null til en verdi
        bool isSoftDelete = false;
        PropertyEntry? deletedAtProp = entry.Properties
            .FirstOrDefault(p => p.Metadata.Name == "DeletedAt");

        if (deletedAtProp is not null && deletedAtProp.OriginalValue is null && deletedAtProp.CurrentValue is not null)
        {
            isSoftDelete = true;
        }

        var details = new Dictionary<string, object?>();

        if (!isSoftDelete)
        {
            var changedFields = new Dictionary<string, object?>();

            foreach (PropertyEntry prop in entry.Properties)
            {
                if (ShouldSkipProperty(prop))
                    continue;

                if (prop.Metadata.Name is "DeletedAt" or "IsActive")
                    continue;

                object? original = prop.OriginalValue;
                object? current = prop.CurrentValue;

                if (!Equals(original, current))
                {
                    changedFields[prop.Metadata.Name] = new { old = original, @new = current };
                }
            }

            if (changedFields.Count > 0)
                details["changed_fields"] = changedFields;
        }

        // Legg til reason fra IAuditContext
        if (auditContext?.Reason is not null)
            details["reason"] = auditContext.Reason;

        // Bestem action
        string action;
        if (isSoftDelete)
        {
            action = $"{ToSnakeCase(entityType)}.delete";
        }
        else if (auditContext?.ActionOverride is not null)
        {
            action = auditContext.ActionOverride;
        }
        else
        {
            action = $"{ToSnakeCase(entityType)}.update";
        }

        // Hopp over vanlige .update-entries som ikke har noe å rapportere
        // (soft-delete og action overrides er alltid meningsfulle selv med tomme details)
        if (details.Count == 0 && !isSoftDelete && auditContext?.ActionOverride is null)
            return null;

        return new AuditLog
        {
            Action = action,
            EntityType = entityType,
            EntityId = GetEntityId(entry),
            UserId = userId,
            UserEmail = userEmail,
            UserName = userName,
            Details = details.Count > 0 ? JsonSerializer.Serialize(details) : null,
        };
    }

    /// <summary>
    /// Bygger en AuditLog for en slettet entitet.
    /// </summary>
    private static AuditLog BuildDeletedEntry(
        EntityEntry entry, string entityType, Guid? userId, string? userEmail, string? userName, DbContext context)
    {
        var details = new Dictionary<string, object?>();

        // Spesiell håndtering av DocumentSignature hard-delete
        if (entityType == "DocumentSignature")
        {
            BuildDocumentSignatureRemovedDetails(entry, details, context);
        }
        else
        {
            // Vanlig hard-delete — capture nøkkelverdier fra original
            foreach (PropertyEntry prop in entry.Properties)
            {
                if (ShouldSkipProperty(prop))
                    continue;

                object? value = prop.OriginalValue;
                if (value is not null)
                    details[prop.Metadata.Name] = value;
            }
        }

        string action = entityType == "DocumentSignature"
            ? "document.signature_removed"
            : $"{ToSnakeCase(entityType)}.delete";

        return new AuditLog
        {
            Action = action,
            EntityType = entityType,
            EntityId = GetEntityId(entry),
            UserId = userId,
            UserEmail = userEmail,
            UserName = userName,
            Details = details.Count > 0 ? JsonSerializer.Serialize(details) : null,
        };
    }

    /// <summary>
    /// Bygger spesielle detaljer for sletting av DocumentSignature.
    /// </summary>
    private static void BuildDocumentSignatureRemovedDetails(
        EntityEntry entry, Dictionary<string, object?> details, DbContext context)
    {
        Guid documentId = GetPropertyValue<Guid>(entry, "DocumentId");
        Guid signatureUserId = GetPropertyValue<Guid>(entry, "UserId");
        DateTime signedAt = GetPropertyValue<DateTime>(entry, "SignedAt");
        int signatureVersion = GetPropertyValue<int>(entry, "SignatureVersion");

        details["document_id"] = documentId;
        details["removed_user_id"] = signatureUserId;
        details["signed_at"] = signedAt;
        details["old_version"] = signatureVersion;

        // Prøv å finne dokumentet i ChangeTracker for tittel og ny versjon
        EntityEntry<Document>? documentEntry = context.ChangeTracker.Entries<Domain.Entities.Documents.Document>()
            .FirstOrDefault(e => e.Entity.Id == documentId);

        if (documentEntry is not null)
        {
            details["document_title"] = documentEntry.Entity.Title;
            details["new_version"] = documentEntry.Entity.Version;
        }

        // Prøv å finne brukeren i ChangeTracker for navn og e-post
        EntityEntry<ApplicationUser>? userEntry = context.ChangeTracker.Entries<Domain.Entities.Identity.ApplicationUser>()
            .FirstOrDefault(e => e.Entity.Id == signatureUserId);

        if (userEntry is not null)
        {
            details["removed_user_name"] = $"{userEntry.Entity.FirstName} {userEntry.Entity.LastName}";
            details["removed_user_email"] = userEntry.Entity.Email;
        }
    }

    /// <summary>
    /// Løser den nåværende brukeren fra HttpContext.
    /// JWT bruker custom "firstName"/"lastName" claims, ikke ClaimTypes.Name.
    /// </summary>
    private static (Guid? userId, string? email, string? name) ResolveCurrentUser(IHttpContextAccessor? httpContextAccessor)
    {
        if (httpContextAccessor?.HttpContext?.User.Identity?.IsAuthenticated != true)
            return (null, null, "System");

        Claim? nameClaim = httpContextAccessor.HttpContext.User.FindFirst(
            ClaimTypes.NameIdentifier);

        if (nameClaim is null || !Guid.TryParse(nameClaim.Value, out Guid userId))
            return (null, null, "System");

        Claim? emailClaim = httpContextAccessor.HttpContext.User.FindFirst(
            ClaimTypes.Email);
        string? email = emailClaim?.Value;

        // JWT bruker custom "firstName"/"lastName" claims, ikke ClaimTypes.Name
        string? firstName = httpContextAccessor.HttpContext.User.FindFirst("firstName")?.Value;
        string? lastName = httpContextAccessor.HttpContext.User.FindFirst("lastName")?.Value;

        string name;
        if (!string.IsNullOrEmpty(firstName) && !string.IsNullOrEmpty(lastName))
            name = $"{firstName} {lastName}";
        else
            name = httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.Name)?.Value ?? "System";

        return (userId, email, name);
    }

    /// <summary>
    /// Sjekker om en egenskap skal ignoreres i revisjonsloggen.
    /// </summary>
    private static bool ShouldSkipProperty(PropertyEntry prop)
    {
        return SkippedProperties.Contains(prop.Metadata.Name)
               || prop.Metadata.IsPrimaryKey()
               || prop.Metadata.IsForeignKey()
               || prop.Metadata.IsShadowProperty();
    }

    /// <summary>
    /// Henter primærnøkkelverdien for en entitet.
    /// </summary>
    private static Guid GetEntityId(EntityEntry entry)
    {
        PropertyEntry? pk = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
        if (pk?.CurrentValue is Guid g1)
            return g1;
        // For deleted entries er CurrentValue null — fall back til OriginalValue
        if (pk?.OriginalValue is Guid g2)
            return g2;

        return Guid.Empty;
    }

    /// <summary>
    /// Henter verdien til en spesifikk egenskap for en entitet.
    /// </summary>
    private static T? GetPropertyValue<T>(EntityEntry entry, string propertyName)
    {
        PropertyEntry? prop = entry.Properties.FirstOrDefault(p => p.Metadata.Name == propertyName);
        if (prop?.OriginalValue is T value)
            return value;

        return default;
    }

    /// <summary>
    /// Konverterer PascalCase til snake_case, f.eks. "CompetencyType" → "competency_type".
    /// Håndterer tall riktig (f.eks. "OtpCode2" → "otp_code2").
    /// </summary>
    private const int MaxSnakeCaseLength = 128;
    private static string ToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name ?? string.Empty;

        var result = new StringBuilder(Math.Min(name.Length + 8, MaxSnakeCaseLength));
        bool wasUpper = false;

        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];
            if (char.IsUpper(c))
            {
                if (i > 0 && (!wasUpper || (i + 1 < name.Length && char.IsLower(name[i + 1]))))
                    result.Append('_');
                result.Append(char.ToLowerInvariant(c));
                wasUpper = true;
            }
            else
            {
                result.Append(c);
                wasUpper = char.IsDigit(c);
            }
        }

        return result.Length > MaxSnakeCaseLength ? result.ToString(0, MaxSnakeCaseLength) : result.ToString();
    }
}