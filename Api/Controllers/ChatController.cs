using KM.RagSandbox.Application.Chat;
using KM.RagSandbox.Domain.Chat;
using Microsoft.AspNetCore.Mvc;

namespace KM.RagSandbox.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly ILlmClient _llmClient;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IChatService chatService, ILlmClient llmClient, ILogger<ChatController> logger)
    {
        _chatService = chatService;
        _llmClient = llmClient;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<ChatResponse>> Chat([FromBody] ChatRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Received chat request");
            var response = await _chatService.ProcessChatAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat request");
            return StatusCode(500, new { error = "An error occurred processing your request" });
        }
    }

    [HttpPost("stream")]
    public async Task ChatStream([FromBody] ChatRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Received streaming chat request");

            Response.Headers.Append("Content-Type", "text/event-stream");
            Response.Headers.Append("Cache-Control", "no-cache");
            Response.Headers.Append("Connection", "keep-alive");

            await foreach (var token in _chatService.ProcessChatStreamAsync(request, cancellationToken))
            {
                var data = $"data: {token}\n\n";
                await Response.WriteAsync(data, cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }

            await Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing streaming chat request");
            await Response.WriteAsync($"data: {{\"error\": \"An error occurred processing your request\"}}\n\n", cancellationToken);
        }
    }

    [HttpGet("models")]
    public async Task<ActionResult<List<OllamaModel>>> GetModels(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Fetching available models");
            var models = await _llmClient.GetAvailableModelsAsync(cancellationToken);
            return Ok(models);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching models");
            return StatusCode(500, new { error = "An error occurred fetching available models" });
        }
    }

    [HttpGet("session/{sessionId}")]
    public ActionResult<ChatSession> GetSession(string sessionId)
    {
        var session = _chatService.GetSession(sessionId);
        if (session == null)
        {
            return NotFound(new { error = "Session not found" });
        }

        return Ok(session);
    }
}
