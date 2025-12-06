namespace KM.RagSandbox.Application.Chat;

public record ChatRequest
{
    public required string Message { get; init; }
    public string? WebUrl { get; init; }
    public string? SessionId { get; init; }
    public string? Model { get; init; }
}

public record ChatResponse
{
    public required string Message { get; init; }
    public required string SessionId { get; init; }
    public string? WebUrl { get; init; }
    public string? Model { get; init; }
}
