global using TUnit.Assertions.Exceptions;
global using TUnit.Assertions.Extensions;
global using TUnit.Core;

internal static class TestHelpers
{
    /// <summary>
    /// Normalizes all line endings to Unix-style (\n) for consistent cross-platform testing.
    /// </summary>
    public static string NormalizeLineEndings(this string value)
    {
        return value.Replace("\r\n", "\n").Replace("\r", "\n");
    }
}
