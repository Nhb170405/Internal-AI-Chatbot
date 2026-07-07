namespace backend_dotnet.Infrastructure.Errors;

public sealed class ApiErrorResponse
{
    public string Code { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string? TraceId { get; set; }
}
