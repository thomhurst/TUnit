using System.Globalization;

namespace TUnit.Assertions.AssertConditions;

internal class BecauseReason(string because)
{
    private string? _message;

    private string CreateMessage()
    {
        const string prefix = "because";
        try
        {
            string message = because.Trim();

            return !message.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                ? $" {prefix} {message}"
                : $" {message}";
        }
        catch (FormatException)
        {
            return $"**WARNING: the because message '{because}' could not be formatted correctly!**";
        }
    }

    public override string ToString()
    {
        _message ??= CreateMessage();
        return _message;
    }
}