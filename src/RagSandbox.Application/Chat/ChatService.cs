using RagSandbox.Application.WebContent;
using RagSandbox.Domain.Chat;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace RagSandbox.Application.Chat;

public class ChatService : IChatService
{
    private readonly ILlmClient _llmClient;
    private readonly IWebScrapingService _webScrapingService;
    private readonly IMessageParser _messageParser;
    private readonly ILogger<ChatService> _logger;
    private readonly ConcurrentDictionary<string, ChatSession> _sessions = new();

    public ChatService(
        ILlmClient llmClient,
        IWebScrapingService webScrapingService,
        IMessageParser messageParser,
        ILogger<ChatService> logger)
    {
        _llmClient = llmClient;
        _webScrapingService = webScrapingService;
        _messageParser = messageParser;
        _logger = logger;
    }

    public async Task<ChatResponse> ProcessChatAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing chat request: {Message}", request.Message);

        // Parse message to extract URLs
        var parseResult = _messageParser.Parse(request.Message);

        // Get or create session
        var session = request.SessionId != null && _sessions.TryGetValue(request.SessionId, out var existingSession)
            ? existingSession
            : new ChatSession();

        // Determine URL to scrape (from request or parsed from message)
        var urlToScrape = request.WebUrl ?? (parseResult.HasUrls ? parseResult.ExtractedUrls.First() : null);

        // If new URL provided, scrape and store content
        if (!string.IsNullOrEmpty(urlToScrape) && urlToScrape != session.WebContentUrl)
        {
            _logger.LogInformation("Scraping new URL: {Url}", urlToScrape);
            var webPage = await _webScrapingService.ScrapeUrlAsync(urlToScrape, cancellationToken);
            
            session.WebContentUrl = urlToScrape;
            
            // Add system message with web content
            var contextMessage = $"You have been provided with the following web page content from {webPage.Url}:\n\n" +
                                $"Title: {webPage.Title}\n\n" +
                                $"Content: {webPage.Content}\n\n" +
                                $"Please answer questions based on this content.";
            
            session.AddMessage("system", contextMessage);
        }

        // Add user message (use cleaned message if URLs were extracted)
        var userMessage = parseResult.HasUrls ? parseResult.CleanedMessage : request.Message;
        if (!string.IsNullOrWhiteSpace(userMessage))
        {
            session.AddMessage("user", userMessage);
        }
        else if (parseResult.HasUrls)
        {
            // If message was only a URL, add a default question
            session.AddMessage("user", "What is this page about?");
        }

        // Build messages for LLM
        var messages = session.Messages.Select(m => new LlmMessage
        {
            Role = m.Role,
            Content = m.Content
        }).ToList();

        // Get LLM response with optional model
        var llmResponse = await _llmClient.GenerateResponseAsync(messages, request.Model, cancellationToken);

        // Add assistant response to session
        session.AddMessage("assistant", llmResponse);

        // Store session
        _sessions[session.Id] = session;

        _logger.LogInformation("Chat request processed. Session: {SessionId}", session.Id);

        return new ChatResponse
        {
            Message = llmResponse,
            SessionId = session.Id,
            WebUrl = urlToScrape,
            Model = request.Model
        };
    }

    public async IAsyncEnumerable<string> ProcessChatStreamAsync(ChatRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing streaming chat request: {Message}", request.Message);

        // Parse message to extract URLs
        var parseResult = _messageParser.Parse(request.Message);

        // Get or create session
        var session = request.SessionId != null && _sessions.TryGetValue(request.SessionId, out var existingSession)
            ? existingSession
            : new ChatSession();

        // Determine URL to scrape (from request or parsed from message)
        var urlToScrape = request.WebUrl ?? (parseResult.HasUrls ? parseResult.ExtractedUrls.First() : null);

        // If new URL provided, scrape and store content
        if (!string.IsNullOrEmpty(urlToScrape) && urlToScrape != session.WebContentUrl)
        {
            _logger.LogInformation("Scraping new URL: {Url}", urlToScrape);
            var webPage = await _webScrapingService.ScrapeUrlAsync(urlToScrape, cancellationToken);

            session.WebContentUrl = urlToScrape;

            // Add system message with web content
            var contextMessage = $"You have been provided with the following web page content from {webPage.Url}:\n\n" +
                                $"Title: {webPage.Title}\n\n" +
                                $"Content: {webPage.Content}\n\n" +
                                $"Please answer questions based on this content.";

            session.AddMessage("system", contextMessage);
        }

        // Add user message (use cleaned message if URLs were extracted)
        var userMessage = parseResult.HasUrls ? parseResult.CleanedMessage : request.Message;
        if (!string.IsNullOrWhiteSpace(userMessage))
        {
            session.AddMessage("user", userMessage);
        }
        else if (parseResult.HasUrls)
        {
            // If message was only a URL, add a default question
            session.AddMessage("user", "What is this page about?");
        }

        // Build messages for LLM
        var messages = session.Messages.Select(m => new LlmMessage
        {
            Role = m.Role,
            Content = m.Content
        }).ToList();

        // Stream LLM response
        var fullResponse = "";
        await foreach (var token in _llmClient.GenerateResponseStreamAsync(messages, request.Model, cancellationToken))
        {
            fullResponse += token;
            yield return token;
        }

        // Add complete assistant response to session
        session.AddMessage("assistant", fullResponse);

        // Store session
        _sessions[session.Id] = session;

        _logger.LogInformation("Streaming chat request processed. Session: {SessionId}", session.Id);
    }

    public ChatSession? GetSession(string sessionId)
    {
        return _sessions.TryGetValue(sessionId, out var session) ? session : null;
    }
}
