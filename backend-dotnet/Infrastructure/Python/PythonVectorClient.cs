using System.Net.Http.Json;
using System.Text.Json;
using backend_dotnet.Contracts.Python;
using Microsoft.Extensions.Options;

namespace backend_dotnet.Infrastructure.Python;

public sealed class PythonVectorClient
{
    private readonly HttpClient _httpClient;
    private readonly PythonServiceOptions _options;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public PythonVectorClient(HttpClient httpClient, IOptions<PythonServiceOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<PythonVectorIndexResponse> IndexDocumentAsync(PythonVectorIndexRequest request, CancellationToken cancellationToken = default)
    {
        // Bai tap Milestone 7:
        // 1. Validate PythonService:BaseUrl khong rong.
        // 2. Validate TimeoutSeconds > 0.
        // 3. Set BaseAddress va Timeout cho HttpClient.
        // 4. POST JSON den endpoint Python "/index-document".
        // 5. Neu HTTP status khong thanh cong:
        //    - doc error body.
        //    - throw InvalidOperationException.
        // 6. Parse JSON thanh PythonVectorIndexResponse.
        // 7. Neu result null thi throw InvalidOperationException.
        // 8. Return result.
        //
        // Goi y:
        // - Pattern giong PythonChunkingClient.ChunkAsync.
        // - Khong log chunk content.
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

        var response = await _httpClient.PostAsJsonAsync("/index-document", request, JsonOptions, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);

            throw new InvalidOperationException(
                $"Python index-document service returned {(int)response.StatusCode}: {errorBody}");
        }

        var result = await response.Content.ReadFromJsonAsync<PythonVectorIndexResponse>(JsonOptions, cancellationToken);

        if (result is null)
        {
            throw new InvalidOperationException("Python index-document service returned empty response.");
        }

        return result;
    }

    public async Task<PythonVectorSearchResponse> SearchAsync(PythonVectorSearchRequest request, CancellationToken cancellationToken = default)
    {
        // Bai tap Milestone 7:
        // 1. Validate PythonService config giong IndexDocumentAsync.
        // 2. POST JSON den endpoint Python "/search".
        // 3. Neu HTTP non-success thi throw InvalidOperationException.
        // 4. Parse JSON thanh PythonVectorSearchResponse.
        // 5. Return result.

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

        var response = await _httpClient.PostAsJsonAsync("/search", request, JsonOptions, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);

            throw new InvalidOperationException(
                $"Python search service returned {(int)response.StatusCode}: {errorBody}");
        }

        var result = await response.Content.ReadFromJsonAsync<PythonVectorSearchResponse>(JsonOptions, cancellationToken);

        if (result is null)
        {
            throw new InvalidOperationException("Python search service returned empty response.");
        }

        return result;
    }
}
