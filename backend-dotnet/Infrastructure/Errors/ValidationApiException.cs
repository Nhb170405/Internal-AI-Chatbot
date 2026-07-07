namespace backend_dotnet.Infrastructure.Errors;

public sealed class ValidationApiException : ApiException
{
    public ValidationApiException(string code, string message)
        : base(StatusCodes.Status400BadRequest, code, message)
    {
    }
}
