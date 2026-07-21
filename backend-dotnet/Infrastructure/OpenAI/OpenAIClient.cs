using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using backend_dotnet.Modules.Assistant.Tools;

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

    // Giu overload cu de ChatService va RagService tiep tuc hoat dong trong luc
    // tool calling dang duoc trien khai tung buoc.
    public Task<OpenAIChatResult> SendChatAsync(
        IReadOnlyList<OpenAIChatMessage> messages,
        CancellationToken cancellationToken = default)
    {
        return SendChatAsync(messages, [], cancellationToken);
    }

    public async Task<OpenAIChatResult> SendChatAsync(
        IReadOnlyList<OpenAIChatMessage> messages,
        IReadOnlyList<AssistantToolDefinition> tools,
        CancellationToken cancellationToken = default)
    {
        // Gui Chat Completions request. Neu tools rong, method hoat dong nhu chat thuong.
        // Neu co tools, model co the tra content hoac tool_calls; method nay chi parse,
        // khong thuc thi tool. Assistant orchestrator se thuc thi tool o lop cao hon.
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

        if (messages.Any(m => string.IsNullOrWhiteSpace(m.Role)))
        {
            throw new InvalidOperationException("Message thieu Role");
        }

        if (messages.Any(m =>
                string.IsNullOrWhiteSpace(m.Content) &&
                (m.ToolCalls is null || m.ToolCalls.Count == 0)))
        {
            throw new InvalidOperationException("Message phai co Content hoac ToolCalls");
        }

        tools ??= [];

        // Dung dictionary de chi them tools/tool_choice khi caller thuc su bat tool calling.
        // Nho vay overload cu van gui payload giong nhu truoc.
        var requestBody = new Dictionary<string, object?>
        {
            ["model"] = _options.ChatModel,
            ["messages"] = messages.Select(m => new Dictionary<string, object?>
            {
                ["role"] = m.Role,
                ["content"] = m.Content,
                ["tool_call_id"] = m.ToolCallId,
                ["tool_calls"] = m.ToolCalls?.Select(call => new
                {
                    id = call.Id,
                    type = "function",
                    function = new
                    {
                        name = call.Name,
                        arguments = call.ArgumentsJson
                    }
                })
            }).ToList()
        };

        if (tools.Count > 0)
        {
            requestBody["tools"] = tools.Select(tool => new
            {
                type = "function",
                function = new
                {
                    name = tool.Name,
                    description = tool.Description,
                    parameters = tool.Parameters,
                    strict = tool.Strict
                }
            }).ToList();
            requestBody["tool_choice"] = "auto";
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, _options.BaseUrl.TrimEnd('/') + "/chat/completions");

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        // Khong gui cac field tool_call_id/tool_calls co gia tri null cho message thuong.
        request.Content = JsonContent.Create(requestBody, options: new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"OpenAI API request failed. StatusCode={(int)response.StatusCode}, Body={TrimForError(responseBody, 500)}");
        }

        var parsed = JsonSerializer.Deserialize<OpenAIChatCompletionResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        var responseMessage = parsed?
            .Choices?
            .FirstOrDefault()?
            .Message;

        var answer = responseMessage?.Content;

        // Chi map function call co du id, name va arguments. Arguments van la JSON string;
        // tung IAssistantTool se deserialize va validate theo schema cua rieng no.
        var toolCalls = responseMessage?
            .ToolCalls?
            .Where(call =>
                string.Equals(call.Type, "function", StringComparison.Ordinal) &&
                !string.IsNullOrWhiteSpace(call.Id) &&
                call.Function is not null &&
                !string.IsNullOrWhiteSpace(call.Function.Name) &&
                !string.IsNullOrWhiteSpace(call.Function.Arguments))
            .Select(call => new OpenAIToolCall
            {
                Id = call.Id!,
                Name = call.Function!.Name!,
                ArgumentsJson = call.Function.Arguments!
            })
            .ToList()
            ?? [];

        if (string.IsNullOrWhiteSpace(answer) && toolCalls.Count == 0)
        {
            throw new InvalidOperationException("OpenAI API response did not contain content or a valid tool call.");
        }

        return new OpenAIChatResult
        {
            Answer = answer ?? string.Empty,
            ToolCalls = toolCalls,
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

        [JsonPropertyName("tool_calls")]
        public List<OpenAIResponseToolCall>? ToolCalls { get; set; }
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

    private sealed class OpenAIResponseToolCall
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("function")]
        public OpenAIResponseFunctionCall? Function { get; set; }
    }

    private sealed class OpenAIResponseFunctionCall
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("arguments")]
        public string? Arguments { get; set; }
    }
}
