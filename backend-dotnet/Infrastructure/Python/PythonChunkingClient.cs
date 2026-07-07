using System.Net.Http.Json;
using System.Text.Json;
using backend_dotnet.Contracts.Python;
using Microsoft.Extensions.Options;

namespace backend_dotnet.Infrastructure.Python;

public sealed class PythonChunkingClient
{
    private readonly HttpClient _httpClient;
    private readonly PythonServiceOptions _options;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public PythonChunkingClient(HttpClient httpClient, IOptions<PythonServiceOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<PythonChunkResponse> ChunkAsync(PythonChunkRequest request, CancellationToken cancellationToken = default)
    {
        // Bai tap Milestone 6:
        // 1. Validate PythonService:BaseUrl khong rong.
        // 2. Validate TimeoutSeconds > 0.
        // 3. Set _httpClient.BaseAddress = new Uri(_options.BaseUrl).
        // 4. Set timeout.
        // 5. POST JSON den endpoint Python "/chunk".
        // 6. Neu HTTP status khong thanh cong:
        //    - doc error body.
        //    - throw InvalidOperationException voi message an toan.
        // 7. Parse JSON thanh PythonChunkResponse.
        // 8. Neu result null thi throw InvalidOperationException.
        // 9. Return result.
        //
        // Goi y:
        // - Pattern gan giong PythonIngestionClient.IngestAsync.
        // - Khong log request.Text vi do la noi dung tai lieu noi bo.

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

        var response = await _httpClient.PostAsJsonAsync("/chunk", request, JsonOptions, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);

            throw new InvalidOperationException(
                $"Python chunking service returned {(int)response.StatusCode}: {errorBody}");
        }

        var result = await response.Content.ReadFromJsonAsync<PythonChunkResponse>(JsonOptions, cancellationToken);

        if (result is null)
        {
            throw new InvalidOperationException("Python chunking service returned empty response.");
        }

        return result;
    }
}
