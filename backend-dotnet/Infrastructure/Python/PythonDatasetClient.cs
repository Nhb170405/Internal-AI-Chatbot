using System.Net.Http.Json;
using System.Text.Json;
using backend_dotnet.Contracts.Python;
using Microsoft.Extensions.Options;

namespace backend_dotnet.Infrastructure.Python;

public sealed class PythonDatasetClient
{
    private readonly HttpClient _httpClient;
    private readonly PythonServiceOptions _options;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public PythonDatasetClient(HttpClient httpClient, IOptions<PythonServiceOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<PythonDatasetProfileResponse> ProfileAsync(PythonDatasetProfileRequest request, CancellationToken cancellationToken = default)
    {
        // Bai tap Milestone 11:
        // 1. Validate PythonService:BaseUrl khong rong.
        // 2. Validate TimeoutSeconds > 0.
        // 3. Set _httpClient.BaseAddress.
        // 4. Set _httpClient.Timeout.
        // 5. POST JSON den "/datasets/profile".
        // 6. Neu HTTP status khong success thi doc error body va throw InvalidOperationException.
        // 7. Parse JSON thanh PythonDatasetProfileResponse.
        // 8. Neu result null thi throw InvalidOperationException.
        // 9. Return result.
        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            throw new InvalidOperationException("PythonService:BaseUrl is not configured.");
        }

        if (_options.TimeoutSeconds <= 0)
        {
            throw new InvalidOperationException("PythonService:TimeoutSeconds is not configured correctly.");
        }

        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);

        var response = await _httpClient.PostAsJsonAsync("/datasets/profile", request, JsonOptions, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Python dataset profile service returned {(int)response.StatusCode}: {errorBody}");
        }

        var result = await response.Content.ReadFromJsonAsync<PythonDatasetProfileResponse>(JsonOptions, cancellationToken);
        if (result == null)
        {
            throw new InvalidOperationException("Python dataset profile service returned empty response.");
        }

        return result;
    }

    public async Task<PythonDatasetAnalysisResponse> AnalyzeAsync(PythonDatasetAnalysisRequest request, CancellationToken cancellationToken = default)
    {
        // Bai tap Milestone 11:
        // 1. Lam giong ProfileAsync nhung endpoint la "/datasets/analyze".
        // 2. Khong log request vi co file path noi bo.
        // 3. Return PythonDatasetAnalysisResponse.
        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            throw new InvalidOperationException("PythonService:BaseUrl is not configured.");
        }

        if (_options.TimeoutSeconds <= 0)
        {
            throw new InvalidOperationException("PythonService:TimeoutSeconds is not configured correctly.");
        }

        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);

        var response = await _httpClient.PostAsJsonAsync("/datasets/analyze", request, JsonOptions, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);

            throw new InvalidOperationException(
                $"Python dataset analysis service returned {(int)response.StatusCode}: {errorBody}");
        }

        var result = await response.Content.ReadFromJsonAsync<PythonDatasetAnalysisResponse>(JsonOptions, cancellationToken);

        if (result is null)
        {
            throw new InvalidOperationException("Python dataset analysis service returned empty response.");
        }

        return result;
    }
}
