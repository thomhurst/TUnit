namespace TUnit.Analyzers.Tests.Extensions;

public static class StringExtensions
{
    /// <summary>
    /// Normalizes line endings to CRLF (Windows format).
    /// This ensures test inputs match the CRLF line endings that Roslyn's
    /// Formatter.FormatAsync() generates by default on all platforms.
    /// </summary>
    public static string NormalizeLineEndings(this string value)
    {
        return value.Replace("\r\n", "\n").Replace("\n", "\r\n");
    }
}
