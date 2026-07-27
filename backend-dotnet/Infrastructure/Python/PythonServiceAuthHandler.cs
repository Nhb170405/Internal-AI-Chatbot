using Microsoft.Extensions.Options;

namespace backend_dotnet.Infrastructure.Python;

public sealed class PythonServiceAuthHandler : DelegatingHandler
{
    public const string HeaderName = "X-Internal-Api-Key";

    private readonly PythonServiceOptions _options;

    public PythonServiceAuthHandler(IOptions<PythonServiceOptions> options)
    {
        _options = options.Value;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("Missing PythonService:ApiKey configuration.");
        }

        request.Headers.Remove(HeaderName);
        request.Headers.Add(HeaderName, _options.ApiKey);

        return base.SendAsync(request, cancellationToken);
    }
}
