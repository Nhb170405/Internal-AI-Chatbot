namespace backend_dotnet.Modules.Audit;

public sealed class AuditLogEntry
{
    public Guid? ActorUserId { get; set; }

    public Guid? ActorGuestSessionId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string ResourceType { get; set; } = string.Empty;

    public string? ResourceId { get; set; }

    public string? MetadataJson { get; set; }

    public string? IpAddress { get; set; }
}
