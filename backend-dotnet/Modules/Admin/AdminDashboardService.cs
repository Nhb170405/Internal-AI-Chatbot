using backend_dotnet.Contracts.Admin;
using backend_dotnet.Infrastructure.Persistence;
using backend_dotnet.Modules.BackgroundJobs;
using backend_dotnet.Modules.Documents;
using Microsoft.EntityFrameworkCore;

namespace backend_dotnet.Modules.Admin;

public sealed class AdminDashboardService
{
    private readonly AppDbContext _db;

    public AdminDashboardService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<AdminOverviewResponse> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        var last24Hours = DateTimeOffset.UtcNow.AddHours(-24);

        var statusCounts = await _db.Documents
            .AsNoTracking()
            .GroupBy(document => document.Status)
            .Select(group => new AdminDocumentStatusCountResponse
            {
                Status = group.Key,
                Count = group.Count()
            })
            .OrderBy(item => item.Status)
            .ToListAsync(cancellationToken);

        var tokenTotals = await _db.TokenUsages
            .AsNoTracking()
            .GroupBy(_ => 1)
            .Select(group => new
            {
                PromptTokens = group.Sum(item => item.PromptTokens ?? 0),
                CompletionTokens = group.Sum(item => item.CompletionTokens ?? 0),
                TotalTokens = group.Sum(item => item.TotalTokens ?? 0)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var recentAuditLogs = await _db.AuditLogs
            .AsNoTracking()
            .OrderByDescending(log => log.CreatedAt)
            .Take(8)
            .Select(log => new AdminRecentAuditLogResponse
            {
                Action = log.Action,
                ResourceType = log.ResourceType,
                CreatedAt = log.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new AdminOverviewResponse
        {
            TotalDocuments = await _db.Documents.CountAsync(cancellationToken),
            IndexedDocuments = await _db.Documents.CountAsync(document => document.Status == DocumentStatus.Indexed, cancellationToken),
            FailedDocuments = await _db.Documents.CountAsync(document => document.Status == DocumentStatus.Failed, cancellationToken),
            DeletedDocuments = await _db.Documents.CountAsync(document => document.Status == DocumentStatus.Deleted, cancellationToken),
            TotalUsers = await _db.Users.CountAsync(cancellationToken),
            ActiveUsers = await _db.Users.CountAsync(user => user.IsActive, cancellationToken),
            TotalAuditLogs = await _db.AuditLogs.CountAsync(cancellationToken),
            AuditLogsLast24Hours = await _db.AuditLogs.CountAsync(log => log.CreatedAt >= last24Hours, cancellationToken),
            TotalChatSessions = await _db.ChatSessions.CountAsync(cancellationToken),
            TotalPromptTokens = tokenTotals?.PromptTokens ?? 0,
            TotalCompletionTokens = tokenTotals?.CompletionTokens ?? 0,
            TotalTokens = tokenTotals?.TotalTokens ?? 0,
            DocumentStatusCounts = statusCounts,
            RecentAuditLogs = recentAuditLogs
        };
    }

    public async Task<AdminUsageResponse> GetUsageAsync(
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken = default)
    {
        var query = _db.TokenUsages.AsNoTracking();

        if (from.HasValue)
        {
            query = query.Where(usage => usage.CreatedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(usage => usage.CreatedAt <= to.Value);
        }

        var totals = await query
            .GroupBy(_ => 1)
            .Select(group => new
            {
                RequestCount = group.Count(),
                PromptTokens = group.Sum(item => item.PromptTokens ?? 0),
                CompletionTokens = group.Sum(item => item.CompletionTokens ?? 0),
                TotalTokens = group.Sum(item => item.TotalTokens ?? 0)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var users = await _db.Users
            .AsNoTracking()
            .Select(user => new { user.Id, user.DisplayName, user.Email })
            .ToDictionaryAsync(user => user.Id, cancellationToken);

        var byActorRaw = await query
            .GroupBy(usage => new { usage.UserId, usage.GuestSessionId })
            .Select(group => new
            {
                group.Key.UserId,
                group.Key.GuestSessionId,
                RequestCount = group.Count(),
                PromptTokens = group.Sum(item => item.PromptTokens ?? 0),
                CompletionTokens = group.Sum(item => item.CompletionTokens ?? 0),
                TotalTokens = group.Sum(item => item.TotalTokens ?? 0)
            })
            .OrderByDescending(item => item.TotalTokens)
            .Take(20)
            .ToListAsync(cancellationToken);

        var byActor = byActorRaw.Select(item =>
        {
            var actorId = item.UserId ?? item.GuestSessionId;
            var actorType = item.UserId.HasValue ? "user" : item.GuestSessionId.HasValue ? "guest" : "unknown";
            var displayName = item.UserId.HasValue && users.TryGetValue(item.UserId.Value, out var user)
                ? $"{user.DisplayName} ({user.Email})"
                : actorType == "guest"
                    ? "Guest session"
                    : "Unknown actor";

            return new AdminUsageByActorResponse
            {
                ActorType = actorType,
                ActorId = actorId,
                DisplayName = displayName,
                RequestCount = item.RequestCount,
                PromptTokens = item.PromptTokens,
                CompletionTokens = item.CompletionTokens,
                TotalTokens = item.TotalTokens
            };
        }).ToList();

        var byModel = await query
            .GroupBy(usage => usage.Model)
            .Select(group => new AdminUsageByModelResponse
            {
                Model = group.Key,
                RequestCount = group.Count(),
                PromptTokens = group.Sum(item => item.PromptTokens ?? 0),
                CompletionTokens = group.Sum(item => item.CompletionTokens ?? 0),
                TotalTokens = group.Sum(item => item.TotalTokens ?? 0)
            })
            .OrderByDescending(item => item.TotalTokens)
            .ToListAsync(cancellationToken);

        var tokenRows = await query
            .Select(usage => new
            {
                usage.CreatedAt,
                PromptTokens = usage.PromptTokens ?? 0,
                CompletionTokens = usage.CompletionTokens ?? 0,
                TotalTokens = usage.TotalTokens ?? 0
            })
            .ToListAsync(cancellationToken);

        var byDay = tokenRows
            .GroupBy(usage => usage.CreatedAt.UtcDateTime.Date)
            .Select(group => new AdminUsageByDayResponse
            {
                Date = group.Key.ToString("yyyy-MM-dd"),
                RequestCount = group.Count(),
                PromptTokens = group.Sum(item => item.PromptTokens),
                CompletionTokens = group.Sum(item => item.CompletionTokens),
                TotalTokens = group.Sum(item => item.TotalTokens)
            })
            .OrderBy(item => item.Date)
            .ToList();

        var auditQuery = _db.AuditLogs.AsNoTracking();

        if (from.HasValue)
        {
            auditQuery = auditQuery.Where(log => log.CreatedAt >= from.Value);
        }

        if (to.HasValue)
        {
            auditQuery = auditQuery.Where(log => log.CreatedAt <= to.Value);
        }

        var auditActions = await auditQuery
            .GroupBy(log => new { log.Action, log.ResourceType })
            .Select(group => new AdminAuditActionUsageResponse
            {
                Action = group.Key.Action,
                ResourceType = group.Key.ResourceType,
                Count = group.Count()
            })
            .OrderByDescending(item => item.Count)
            .Take(30)
            .ToListAsync(cancellationToken);

        var documentStatusCounts = await _db.Documents
            .AsNoTracking()
            .GroupBy(document => document.Status)
            .Select(group => new AdminStatusCountResponse
            {
                Status = group.Key,
                Count = group.Count()
            })
            .OrderBy(item => item.Status)
            .ToListAsync(cancellationToken);

        var backgroundJobStatusCounts = await _db.DocumentProcessingJobs
            .AsNoTracking()
            .GroupBy(job => job.Status)
            .Select(group => new AdminStatusCountResponse
            {
                Status = group.Key,
                Count = group.Count()
            })
            .OrderBy(item => item.Status)
            .ToListAsync(cancellationToken);

        var recentFailedJobs = await _db.DocumentProcessingJobs
            .AsNoTracking()
            .Where(job => job.Status == DocumentProcessingJobStatus.Failed)
            .OrderByDescending(job => job.UpdatedAt)
            .Take(10)
            .Select(job => new AdminFailedJobResponse
            {
                JobId = job.Id,
                DocumentId = job.DocumentId,
                LastError = job.LastError,
                UpdatedAt = job.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return new AdminUsageResponse
        {
            From = from,
            To = to,
            TotalRequests = totals?.RequestCount ?? 0,
            TotalPromptTokens = totals?.PromptTokens ?? 0,
            TotalCompletionTokens = totals?.CompletionTokens ?? 0,
            TotalTokens = totals?.TotalTokens ?? 0,
            ByActor = byActor,
            ByModel = byModel,
            ByDay = byDay,
            AuditActions = auditActions,
            DocumentStatusCounts = documentStatusCounts,
            BackgroundJobStatusCounts = backgroundJobStatusCounts,
            RecentFailedJobs = recentFailedJobs
        };
    }
}
