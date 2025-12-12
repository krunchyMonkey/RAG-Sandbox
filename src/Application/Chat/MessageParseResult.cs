namespace KM.RagSandbox.Application.Chat;

public record MessageParseResult
{
    public required string CleanedMessage { get; init; }
    public List<string> ExtractedUrls { get; init; } = new();
    public bool HasUrls => ExtractedUrls.Count > 0;
}
