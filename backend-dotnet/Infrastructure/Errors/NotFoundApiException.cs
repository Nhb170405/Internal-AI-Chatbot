namespace backend_dotnet.Infrastructure.Errors;

public sealed class NotFoundApiException : ApiException
{
    public NotFoundApiException(string code, string message)
        : base(StatusCodes.Status404NotFound, code, message)
    {
    }
}
