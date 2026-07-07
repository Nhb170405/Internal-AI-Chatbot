namespace backend_dotnet.Infrastructure.Errors;

public sealed class ForbiddenApiException : ApiException
{
    public ForbiddenApiException(string code, string message)
        : base(StatusCodes.Status403Forbidden, code, message)
    {
    }
}
