namespace KM.RagSandbox.Domain.Chat;

public class ChatSession
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public List<ChatMessage> Messages { get; init; } = new();
    public string? WebContentUrl { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public void AddMessage(string role, string content)
    {
        Messages.Add(new ChatMessage 
        { 
            Role = role, 
            Content = content 
        });
    }
}
