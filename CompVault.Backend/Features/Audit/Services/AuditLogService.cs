using CompVault.Backend.Infrastructure.Data;
using CompVault.Shared.DTOs.Audit;
using CompVault.Shared.Result;

using Microsoft.EntityFrameworkCore;

namespace CompVault.Backend.Features.Audit.Services;

/// <inheritdoc />
public sealed class AuditLogService(AppDbContext dbContext) : IAuditLogService
{
    private const int MaxPageSize = 100;

    /// <inheritdoc />
    public async Task<Result<PagedResult<AuditLogDto>>> GetAsync(
        AuditLogQueryParameters parameters, CancellationToken cancellationToken = default)
    {
        int pageSize = Math.Min(Math.Max(parameters.PageSize, 1), MaxPageSize);
        int page = Math.Max(parameters.Page, 1);

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
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(AuditLogMapper.ToDto).ToList();

        return Result<PagedResult<AuditLogDto>>.Success(new PagedResult<AuditLogDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
        });
    }
}