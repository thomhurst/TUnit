namespace TUnit.Analyzers.Tests.Extensions;

public static class StringExtensions
{
    /// <summary>
    /// Normalizes line endings to LF (Unix format) for cross-platform consistency.
    /// This ensures test inputs use consistent line endings regardless of the platform
    /// where the test string literals are defined, preventing CRLF/LF mismatches between
    /// Windows and Unix systems.
    /// </summary>
    public static string NormalizeLineEndings(this string value)
    {
        return value.Replace("\r\n", "\n");
    }
}
