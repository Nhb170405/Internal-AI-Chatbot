namespace backend_dotnet.Infrastructure.Errors;

public sealed class ConflictApiException : ApiException
{
    public ConflictApiException(string code, string message)
        : base(StatusCodes.Status409Conflict, code, message)
    {
    }
}
