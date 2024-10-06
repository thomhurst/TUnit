using System.Globalization;

namespace TUnit.Assertions.AssertConditions;

internal class BecauseReason(string reason)
{
    private string? _message;

    private string CreateMessage()
    {
        const string prefix = "because";
        string message = reason.Trim();

        var messageWithoutPrefix = message.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? message.Substring(prefix.Length).Trim()
            : message;
        return $"Because: {messageWithoutPrefix}";
    }

    public override string ToString()
    {
        _message ??= CreateMessage();
        return _message;
    }
}