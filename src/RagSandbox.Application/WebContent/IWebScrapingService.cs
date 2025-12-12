namespace RagSandbox.Application.WebContent;

public interface IWebScrapingService
{
    Task<Domain.WebContent.WebPage> ScrapeUrlAsync(string url, CancellationToken cancellationToken = default);
}
