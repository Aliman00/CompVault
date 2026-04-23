using CompVault.Backend.Infrastructure.Data;
using CompVault.Shared.DTOs.Audit;
using CompVault.Shared.DTOs.Common.Pagination;
using CompVault.Shared.Result;

using Microsoft.EntityFrameworkCore;

namespace CompVault.Backend.Features.Audit.Services;

/// <inheritdoc />
public sealed class AuditLogService(AppDbContext dbContext) : IAuditLogService
{
    /// <inheritdoc />
    public async Task<Result<PagedResult<AuditLogDto>>> GetAsync(
        AuditLogQueryParameters parameters, CancellationToken cancellationToken = default)
    {
        IQueryable<Domain.Entities.Audit.AuditLog> query = dbContext.AuditLogs.AsNoTracking();

        // Filtrering
        if (!string.IsNullOrWhiteSpace(parameters.Action))
            query = query.Where(a => a.Action == parameters.Action);

        if (!string.IsNullOrWhiteSpace(parameters.EntityType))
            query = query.Where(a => a.EntityType == parameters.EntityType);

        if (parameters.EntityId.HasValue)
            query = query.Where(a => a.EntityId == parameters.EntityId.Value);

        if (parameters.UserId.HasValue)
            query = query.Where(a => a.UserId == parameters.UserId.Value);

        if (parameters.From.HasValue)
            query = query.Where(a => a.CreatedAt >= parameters.From.Value);

        if (parameters.To.HasValue)
            query = query.Where(a => a.CreatedAt < parameters.To.Value);

        // Totalt antall før paginering
        int totalCount = await query.CountAsync(cancellationToken);

        // Paginering og sortering
        List<Domain.Entities.Audit.AuditLog> items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip(parameters.Skip)
            .Take(parameters.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(AuditLogMapper.ToDto).ToList();

        return Result<PagedResult<AuditLogDto>>.Success(PagedResult<AuditLogDto>.Create(dtos, totalCount, parameters));
    }
}