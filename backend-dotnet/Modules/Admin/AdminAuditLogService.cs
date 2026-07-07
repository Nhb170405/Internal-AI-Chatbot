using backend_dotnet.Contracts.Admin;
using backend_dotnet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace backend_dotnet.Modules.Admin;

public sealed class AdminAuditLogService
{
    private readonly AppDbContext _db;

    public AdminAuditLogService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<AdminAuditLogsResponse> ListAsync(
        string? action,
        string? resourceType,
        string? actorId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(action))
        {
            var normalizedAction = action.Trim();
            query = query.Where(log => log.Action.Contains(normalizedAction));
        }

        if (!string.IsNullOrWhiteSpace(resourceType))
        {
            var normalizedResourceType = resourceType.Trim();
            query = query.Where(log => log.ResourceType == normalizedResourceType);
        }

        if (Guid.TryParse(actorId, out var actorGuid))
        {
            query = query.Where(log =>
                log.ActorUserId == actorGuid ||
                log.ActorGuestSessionId == actorGuid);
        }

        if (from.HasValue)
        {
            query = query.Where(log => log.CreatedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(log => log.CreatedAt <= to.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(log => log.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(log => new AdminAuditLogItemResponse
            {
                Id = log.Id,
                ActorUserId = log.ActorUserId,
                ActorGuestSessionId = log.ActorGuestSessionId,
                Action = log.Action,
                ResourceType = log.ResourceType,
                ResourceId = log.ResourceId,
                MetadataJson = log.MetadataJson,
                IpAddress = log.IpAddress,
                CreatedAt = log.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new AdminAuditLogsResponse
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = items
        };
    }
}
