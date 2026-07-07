namespace backend_dotnet.Infrastructure.Errors;

public sealed class UnauthorizedApiException : ApiException
{
    public UnauthorizedApiException(string code, string message)
        : base(StatusCodes.Status401Unauthorized, code, message)
    {
    }
}
