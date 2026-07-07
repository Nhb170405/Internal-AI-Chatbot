using backend_dotnet.Infrastructure.Persistence;
using System.Text.Json;

namespace backend_dotnet.Modules.Audit;

public sealed class AuditLogService
{
    private const int MaxMetadataStringLength = 500;
    private const int MaxMetadataJsonLength = 4000;
    private const int MaxArrayItems = 20;

    private readonly AppDbContext _db;
    public AuditLogService(AppDbContext db)
    {
        _db = db;
    }
    public async Task LogAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
    {
        // TODO:
        // 1. Tao AuditLog.Id = Guid moi.
        // 2. Copy cac field tu AuditLogEntry.
        // 3. Set CreatedAt = thoi gian hien tai.
        // 4. Luu vao SQL Server.
        //
        // Luu y bao mat:
        // - Khong luu password, cookie, token, API key.
        // - Neu login fail, co the luu email da mask trong MetadataJson.

        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorUserId = entry.ActorUserId,
            ActorGuestSessionId = entry.ActorGuestSessionId,
            Action = entry.Action,
            ResourceType = entry.ResourceType,
            ResourceId = entry.ResourceId,
            // metadata khong luu thong tin nhay cam nen phai loc
            MetadataJson = SanitizeMetadataJson(entry.MetadataJson),
            IpAddress = entry.IpAddress,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.AuditLogs.Add(auditLog);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static string? SanitizeMetadataJson(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(metadataJson);

            var safeMetadata = new Dictionary<string, object?>();

            foreach (var property in document.RootElement.EnumerateObject())
            {
                var key = property.Name;
                safeMetadata[key] = ConvertJsonElement(property.Value, key);
            }

            var serialized = JsonSerializer.Serialize(safeMetadata);

            if (serialized.Length <= MaxMetadataJsonLength)
            {
                return serialized;
            }

            return JsonSerializer.Serialize(new
            {
                truncated = true,
                originalLength = serialized.Length
            });
        }
        catch
        {
            return null;
        }
    }

    private static bool IsSensitiveKey(string key)
    {
        var normalizedKey = key.Trim().ToLowerInvariant();

        return normalizedKey.Contains("password")
            || normalizedKey.Contains("token")
            || normalizedKey.Contains("cookie")
            || normalizedKey.Contains("secret")
            || normalizedKey.Contains("api_key")
            || normalizedKey.Contains("apikey")
            || normalizedKey.Contains("authorization")
            || normalizedKey.Contains("credential")
            || normalizedKey.Contains("connectionstring")
            || normalizedKey.Contains("connection_string")
            || normalizedKey.Contains("sessionkey")
            || normalizedKey.Contains("storagepath")
            || normalizedKey.Contains("filepath")
            || normalizedKey.Contains("fullpath")
            || normalizedKey.Contains("prompt")
            || normalizedKey.Contains("ragcontext")
            || normalizedKey == "context"
            || normalizedKey == "query"
            || normalizedKey.Contains("searchquery")
            || normalizedKey.Contains("extractedtext")
            || normalizedKey.Contains("chunkcontent")
            || normalizedKey.Contains("rawcontent")
            || normalizedKey == "content";
    }

    private static object? ConvertJsonElement(JsonElement element, string? key = null)
    {
        if (!string.IsNullOrWhiteSpace(key) && IsSensitiveKey(key))
        {
            return "[REDACTED]";
        }

        return element.ValueKind switch
        {
            JsonValueKind.String => TruncateString(element.GetString()),
            JsonValueKind.Number => element.TryGetInt64(out var longValue)
                ? longValue
                : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Object => ConvertObject(element),
            JsonValueKind.Array => ConvertArray(element),
            _ => TruncateString(element.ToString())
        };
    }

    private static Dictionary<string, object?> ConvertObject(JsonElement element)
    {
        var result = new Dictionary<string, object?>();

        foreach (var property in element.EnumerateObject())
        {
            result[property.Name] = ConvertJsonElement(property.Value, property.Name);
        }

        return result;
    }

    private static List<object?> ConvertArray(JsonElement element)
    {
        var result = new List<object?>();
        var itemCount = 0;

        foreach (var item in element.EnumerateArray())
        {
            if (itemCount >= MaxArrayItems)
            {
                result.Add("[TRUNCATED_ARRAY]");
                break;
            }

            result.Add(ConvertJsonElement(item));
            itemCount++;
        }

        return result;
    }

    private static string? TruncateString(string? value)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= MaxMetadataStringLength)
        {
            return value;
        }

        return value[..MaxMetadataStringLength] + "...[TRUNCATED]";
    }
}
