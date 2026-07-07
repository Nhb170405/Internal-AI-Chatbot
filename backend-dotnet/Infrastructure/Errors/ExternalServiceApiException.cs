namespace backend_dotnet.Infrastructure.Errors;

public sealed class ExternalServiceApiException : ApiException
{
    public ExternalServiceApiException(string code, string message)
        : base(StatusCodes.Status502BadGateway, code, message)
    {
    }
}
