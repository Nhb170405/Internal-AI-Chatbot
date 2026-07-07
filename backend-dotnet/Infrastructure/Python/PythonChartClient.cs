using System.Net.Http.Json;
using System.Text.Json;
using backend_dotnet.Contracts.Python;
using Microsoft.Extensions.Options;

namespace backend_dotnet.Infrastructure.Python;

public sealed class PythonChartClient
{
    private readonly HttpClient _httpClient;
    private readonly PythonServiceOptions _options;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public PythonChartClient(HttpClient httpClient, IOptions<PythonServiceOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<PythonChartRenderResponse> RenderAsync(PythonChartRenderRequest request, CancellationToken cancellationToken = default)
    {
        // Bai tap Milestone 12:
        // 1. Validate PythonService:BaseUrl khong rong.
        // 2. Validate TimeoutSeconds > 0.
        // 3. Set _httpClient.BaseAddress va Timeout.
        // 4. POST JSON den "/charts/render".
        // 5. Neu HTTP status khong success:
        //    - doc error body.
        //    - throw InvalidOperationException.
        // 6. Parse JSON thanh PythonChartRenderResponse.
        // 7. Neu result null thi throw InvalidOperationException.
        // 8. Return result.
        //
        // Luu y:
        // - Khong gui filePath trong request nay.
        // - Data co the chua thong tin noi bo, khong log full data.
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

        var response = await _httpClient.PostAsJsonAsync("/charts/render", request, JsonOptions, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);

            throw new InvalidOperationException(
                $"Python chart render service returned {(int)response.StatusCode}: {errorBody}");
        }

        var result = await response.Content.ReadFromJsonAsync<PythonChartRenderResponse>(JsonOptions, cancellationToken);

        if (result is null)
        {
            throw new InvalidOperationException("Python chart render service returned empty response.");
        }

        return result;

    }
}
