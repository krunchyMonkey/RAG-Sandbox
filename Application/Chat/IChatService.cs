using KM.RagSandbox.Domain.Chat;

namespace KM.RagSandbox.Application.Chat;

public interface IChatService
{
    Task<ChatResponse> ProcessChatAsync(ChatRequest request, CancellationToken cancellationToken = default);
    IAsyncEnumerable<string> ProcessChatStreamAsync(ChatRequest request, CancellationToken cancellationToken = default);
    ChatSession? GetSession(string sessionId);
}
