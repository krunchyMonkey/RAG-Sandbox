namespace KM.RagSandbox.Application.Chat;

public interface IMessageParser
{
    MessageParseResult Parse(string message);
}
