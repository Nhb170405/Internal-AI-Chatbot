using System.Net.Http.Json;
using backend_dotnet.Contracts.Python;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace backend_dotnet.Infrastructure.Python;

public sealed class PythonIngestionClient
{
    private readonly HttpClient _httpClient;
    private readonly PythonServiceOptions _options;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public PythonIngestionClient(HttpClient httpClient, IOptions<PythonServiceOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<PythonIngestResponse> IngestAsync(PythonIngestRequest request, CancellationToken cancellationToken = default)
    {
        // Muc tieu:
        // 1. Validate PythonService:BaseUrl khong rong.
        // 2. Set BaseAddress/timeout cho HttpClient neu can.
        // 3. POST JSON den /ingest cua ai-service-python.
        // 4. Neu Python tra non-success HTTP thi throw InvalidOperationException.
        // 5. Parse JSON thanh PythonIngestResponse.
        // 6. Neu response null thi throw InvalidOperationException.
        //
        // Goi y:
        // - new Uri(_options.BaseUrl)
        // - _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds)
        // - await _httpClient.PostAsJsonAsync("/ingest", request, cancellationToken)
        // - await response.Content.ReadFromJsonAsync<PythonIngestResponse>(...)
        //
        // Khong log request vi co file path noi bo.
        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            throw new InvalidOperationException("Missing PythonService:BaseUrl configuration.");
        }

        _httpClient.BaseAddress = new Uri(_options.BaseUrl);

        if (_options.TimeoutSeconds <= 0)
        {
            throw new InvalidOperationException("PythonService:TimeoutSeconds must be greater than 0.");
        }
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);

        var response = await _httpClient.PostAsJsonAsync("/ingest", request, JsonOptions, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Python ingestion service returned {(int)response.StatusCode}: {errorBody}");
        }

        var result = await response.Content.ReadFromJsonAsync<PythonIngestResponse>(JsonOptions, cancellationToken);

        if (result is null)
        {
            throw new InvalidOperationException("Python ingestion service returned empty response.");
        }

        return result;
    }
}
