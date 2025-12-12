using RagSandbox.Application.Chat;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RagSandbox.Infrastructure.LLM;

public class OllamaClient : ILlmClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaClient> _logger;
    private readonly string _defaultModel;

    public OllamaClient(HttpClient httpClient, IConfiguration configuration, ILogger<OllamaClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _defaultModel = configuration["Ollama:DefaultModel"] ?? "llama3.2:latest";
    }

    public async Task<string> GenerateResponseAsync(List<LlmMessage> messages, string? model = null, CancellationToken cancellationToken = default)
    {
        var selectedModel = model ?? _defaultModel;

        try
        {
            _logger.LogInformation("Generating response using model: {Model}", selectedModel);

            // Build prompt from messages
            var prompt = BuildPrompt(messages);

            var request = new
            {
                model = selectedModel,
                prompt = prompt,
                stream = false
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/generate", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<OllamaGenerateResponse>(responseJson);

            if (result?.Response == null)
            {
                throw new InvalidOperationException("Invalid response from Ollama");
            }

            _logger.LogInformation("Response generated successfully");
            return result.Response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating response from LLM with model {Model}", selectedModel);
            throw;
        }
    }

    public async IAsyncEnumerable<string> GenerateResponseStreamAsync(List<LlmMessage> messages, string? model = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var selectedModel = model ?? _defaultModel;

        _logger.LogInformation("Generating streaming response using model: {Model}", selectedModel);

        // Build prompt from messages
        var prompt = BuildPrompt(messages);

        var request = new
        {
            model = selectedModel,
            prompt = prompt,
            stream = true
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/api/generate")
        {
            Content = content
        };

        using var response = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(line))
                continue;

            var chunk = JsonSerializer.Deserialize<OllamaStreamResponse>(line);

            if (chunk?.Response != null)
            {
                yield return chunk.Response;
            }

            if (chunk?.Done == true)
            {
                break;
            }
        }

        _logger.LogInformation("Streaming response completed");
    }

    public async Task<List<OllamaModel>> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching available models");

            var response = await _httpClient.GetAsync("/api/tags", cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<OllamaTagsResponse>(responseJson);

            if (result?.Models == null)
            {
                return new List<OllamaModel>();
            }

            var models = result.Models.Select(m => new OllamaModel
            {
                Name = m.Name ?? "unknown",
                Id = m.Digest ?? "",
                Size = FormatBytes(m.Size),
                Modified = m.ModifiedAt?.ToString("g") ?? "unknown"
            }).ToList();

            _logger.LogInformation("Found {Count} available models", models.Count);
            return models;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching available models");
            throw;
        }
    }

    private string BuildPrompt(List<LlmMessage> messages)
    {
        var sb = new StringBuilder();
        
        foreach (var message in messages)
        {
            switch (message.Role.ToLower())
            {
                case "system":
                    sb.AppendLine($"System: {message.Content}");
                    sb.AppendLine();
                    break;
                case "user":
                    sb.AppendLine($"User: {message.Content}");
                    sb.AppendLine();
                    break;
                case "assistant":
                    sb.AppendLine($"Assistant: {message.Content}");
                    sb.AppendLine();
                    break;
            }
        }
        
        sb.Append("Assistant: ");
        return sb.ToString();
    }

    private static string FormatBytes(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int suffixIndex = 0;
        double size = bytes;

        while (size >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            size /= 1024;
            suffixIndex++;
        }

        return $"{size:0.#} {suffixes[suffixIndex]}";
    }

    private class OllamaGenerateResponse
    {
        [JsonPropertyName("response")]
        public string? Response { get; set; }
    }

    private class OllamaStreamResponse
    {
        [JsonPropertyName("response")]
        public string? Response { get; set; }

        [JsonPropertyName("done")]
        public bool Done { get; set; }
    }

    private class OllamaTagsResponse
    {
        [JsonPropertyName("models")]
        public List<OllamaModelInfo>? Models { get; set; }
    }

    private class OllamaModelInfo
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("digest")]
        public string? Digest { get; set; }

        [JsonPropertyName("size")]
        public long Size { get; set; }

        [JsonPropertyName("modified_at")]
        public DateTime? ModifiedAt { get; set; }
    }
}
