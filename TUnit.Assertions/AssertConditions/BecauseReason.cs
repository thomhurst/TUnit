namespace TUnit.Assertions.AssertConditions;

internal class BecauseReason(string reason)
{
    private string? _message;

    private string CreateMessage()
    {
        const string prefix = "because";
        string message = reason.Trim();

        return !message.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? $", {prefix} {message}"
            : $", {message}";
    }

    public override string ToString()
    {
        _message ??= CreateMessage();
        return _message;
    }
}