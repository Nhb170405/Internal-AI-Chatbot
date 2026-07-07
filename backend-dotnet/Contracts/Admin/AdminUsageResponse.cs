namespace backend_dotnet.Contracts.Admin;

public sealed class AdminUsageResponse
{
    public DateTimeOffset? From { get; set; }

    public DateTimeOffset? To { get; set; }

    public int TotalRequests { get; set; }

    public int TotalPromptTokens { get; set; }

    public int TotalCompletionTokens { get; set; }

    public int TotalTokens { get; set; }

    public List<AdminUsageByActorResponse> ByActor { get; set; } = [];

    public List<AdminUsageByModelResponse> ByModel { get; set; } = [];

    public List<AdminUsageByDayResponse> ByDay { get; set; } = [];

    public List<AdminAuditActionUsageResponse> AuditActions { get; set; } = [];

    public List<AdminStatusCountResponse> DocumentStatusCounts { get; set; } = [];

    public List<AdminStatusCountResponse> BackgroundJobStatusCounts { get; set; } = [];

    public List<AdminFailedJobResponse> RecentFailedJobs { get; set; } = [];
}

public sealed class AdminUsageByActorResponse
{
    public string ActorType { get; set; } = string.Empty;

    public Guid? ActorId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public int RequestCount { get; set; }

    public int PromptTokens { get; set; }

    public int CompletionTokens { get; set; }

    public int TotalTokens { get; set; }
}

public sealed class AdminUsageByModelResponse
{
    public string Model { get; set; } = string.Empty;

    public int RequestCount { get; set; }

    public int PromptTokens { get; set; }

    public int CompletionTokens { get; set; }

    public int TotalTokens { get; set; }
}

public sealed class AdminUsageByDayResponse
{
    public string Date { get; set; } = string.Empty;

    public int RequestCount { get; set; }

    public int PromptTokens { get; set; }

    public int CompletionTokens { get; set; }

    public int TotalTokens { get; set; }
}

public sealed class AdminAuditActionUsageResponse
{
    public string Action { get; set; } = string.Empty;

    public string ResourceType { get; set; } = string.Empty;

    public int Count { get; set; }
}

public sealed class AdminStatusCountResponse
{
    public string Status { get; set; } = string.Empty;

    public int Count { get; set; }
}

public sealed class AdminFailedJobResponse
{
    public Guid JobId { get; set; }

    public Guid DocumentId { get; set; }

    public string? LastError { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
