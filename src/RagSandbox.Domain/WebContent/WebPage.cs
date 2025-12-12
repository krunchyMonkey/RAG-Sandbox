namespace RagSandbox.Domain.WebContent;

public class WebPage
{
    public required string Url { get; init; }
    public required string Title { get; init; }
    public required string Content { get; init; }
    public DateTime ScrapedAt { get; init; } = DateTime.UtcNow;
}
