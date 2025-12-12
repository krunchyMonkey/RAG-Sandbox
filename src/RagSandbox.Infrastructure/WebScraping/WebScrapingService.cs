using HtmlAgilityPack;
using RagSandbox.Application.WebContent;

namespace RagSandbox.Infrastructure.WebScraping;

public class WebScrapingService : IWebScrapingService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WebScrapingService> _logger;

    public WebScrapingService(HttpClient httpClient, ILogger<WebScrapingService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<Domain.WebContent.WebPage> ScrapeUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Scraping URL: {Url}", url);
            
            var html = await _httpClient.GetStringAsync(url, cancellationToken);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Extract title
            var title = doc.DocumentNode.SelectSingleNode("//title")?.InnerText?.Trim() 
                       ?? "Untitled";

            // Extract main content - prioritize common content containers
            var contentNodes = doc.DocumentNode.SelectNodes("//article | //main | //div[@class='content'] | //div[@id='content'] | //body");
            var content = contentNodes?.FirstOrDefault()?.InnerText ?? doc.DocumentNode.InnerText;

            // Clean up the content
            content = System.Text.RegularExpressions.Regex.Replace(content, @"\s+", " ").Trim();

            _logger.LogInformation("Successfully scraped URL: {Url}, Content length: {Length}", url, content.Length);

            return new Domain.WebContent.WebPage
            {
                Url = url,
                Title = title,
                Content = content
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scraping URL: {Url}", url);
            throw;
        }
    }
}
