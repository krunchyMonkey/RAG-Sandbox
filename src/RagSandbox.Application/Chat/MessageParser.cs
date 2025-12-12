using System.Text.RegularExpressions;

namespace RagSandbox.Application.Chat;

public partial class MessageParser : IMessageParser
{
    [GeneratedRegex(@"https?://[^\s<>""{}|\\^`\[\]]+", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex UrlRegex();

    public MessageParseResult Parse(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return new MessageParseResult
            {
                CleanedMessage = string.Empty,
                ExtractedUrls = new List<string>()
            };
        }

        var urls = new List<string>();
        var matches = UrlRegex().Matches(message);

        foreach (Match match in matches)
        {
            // Trim common trailing punctuation that often follows URLs in text
            var url = match.Value.TrimEnd(',', '.', ';', '!', '?', ')', ':');
            urls.Add(url);
        }

        // Optionally remove URLs from the message to clean it up
        var cleanedMessage = UrlRegex().Replace(message, "").Trim();
        
        // If message only contained URLs, keep original message
        if (string.IsNullOrWhiteSpace(cleanedMessage) && urls.Count > 0)
        {
            cleanedMessage = message;
        }

        return new MessageParseResult
        {
            CleanedMessage = cleanedMessage,
            ExtractedUrls = urls
        };
    }
}
