namespace KM.RagSandbox.Application.Chat;

public record LlmMessage
{
    public required string Role { get; init; }
    public required string Content { get; init; }
}

public record OllamaModel
{
    public required string Name { get; init; }
    public required string Id { get; init; }
    public required string Size { get; init; }
    public required string Modified { get; init; }
}

public interface ILlmClient
{
    Task<string> GenerateResponseAsync(List<LlmMessage> messages, string? model = null, CancellationToken cancellationToken = default);
    IAsyncEnumerable<string> GenerateResponseStreamAsync(List<LlmMessage> messages, string? model = null, CancellationToken cancellationToken = default);
    Task<List<OllamaModel>> GetAvailableModelsAsync(CancellationToken cancellationToken = default);
}
