using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace backend_dotnet.Infrastructure.OpenAI;

public sealed class OpenAIClient
{
    private readonly HttpClient _httpClient;
    private readonly OpenAIOptions _options;

    public OpenAIClient(HttpClient httpClient, OpenAIOptions options)
    {
        _httpClient = httpClient;
        _options = options;
    }

    public async Task<OpenAIChatResult> SendChatAsync(IReadOnlyList<OpenAIChatMessage> messages, CancellationToken cancellationToken = default)
    {
        // TODO:
        // 1. Validate _options.ApiKey khong rong.
        // 2. Validate _options.ChatModel khong rong.
        // 3. Tao HTTP request toi OpenAI chat endpoint.
        // 4. Set Authorization header:
        //    Bearer {ApiKey}
        // 5. Tao JSON body gom:
        //    - model
        //    - messages
        // 6. Gui request bang _httpClient.
        // 7. Neu OpenAI tra loi loi:
        //    - doc status code/body an toan.
        //    - throw exception ro rang cho ChatService/Controller xu ly.
        // 8. Parse response:
        //    - answer tu choices[0].message.content
        //    - usage.prompt_tokens
        //    - usage.completion_tokens
        //    - usage.total_tokens
        // 9. Return OpenAIChatResult.
        //
        // Luu y:
        // - Khong log API key.
        // - Khong gui chat history dai trong Milestone 2.
        // - Tool calling/RAG de milestone sau.
        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            throw new InvalidOperationException("Thieu BaseUrl");
        }

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("Thieu API Key");
        }

        if (string.IsNullOrWhiteSpace(_options.ChatModel))
        {
            throw new InvalidOperationException("Thieu ChatModel");
        }

        if (messages is null || messages.Count == 0)
        {
            throw new InvalidOperationException("Thieu Message");
        }

        if (messages.Any(m => string.IsNullOrWhiteSpace(m.Role) || string.IsNullOrWhiteSpace(m.Content)))
        {
            throw new InvalidOperationException("Message thieu Role");
        }

        var requestBody = new
        {
            model = _options.ChatModel,
            messages = messages.Select(m => new
            {
                role = m.Role,
                content = m.Content
            })
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, _options.BaseUrl.TrimEnd('/') + "/chat/completions");

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        request.Content = JsonContent.Create(requestBody);

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"OpenAI API request failed. StatusCode={(int)response.StatusCode}, Body={TrimForError(responseBody, 500)}");
        }

        var parsed = JsonSerializer.Deserialize<OpenAIChatCompletionResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        var answer = parsed?
            .Choices?
            .FirstOrDefault()?
            .Message?
            .Content;


        if (string.IsNullOrWhiteSpace(answer))
        {
            throw new InvalidOperationException("OpenAI API response did not contain an answer.");
        }

        return new OpenAIChatResult
        {
            Answer = answer,
            PromptTokens = parsed?.Usage?.PromptTokens ?? 0,
            CompletionTokens = parsed?.Usage?.CompletionTokens ?? 0,
            TotalTokens = parsed?.Usage?.TotalTokens ?? 0,
            Model = parsed?.Model ?? _options.ChatModel
        };
    }

    private static string TrimForError(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Length <= maxLength
            ? value
            : value[..maxLength] + "...";
    }

    private sealed class OpenAIChatCompletionResponse
    {
        [JsonPropertyName("choices")]
        public List<OpenAIChoice>? Choices { get; set; }

        [JsonPropertyName("usage")]
        public OpenAIUsage? Usage { get; set; }

        [JsonPropertyName("model")]
        public string? Model { get; set; }
    }

    private sealed class OpenAIChoice
    {
        [JsonPropertyName("message")]
        public OpenAIResponseMessage? Message { get; set; }
    }

    private sealed class OpenAIResponseMessage
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }

    private sealed class OpenAIUsage
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }

        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
    }
}
