namespace TUnit.Analyzers.Tests.Extensions;

public static class StringExtensions
{
    /// <summary>
    /// Normalizes line endings to the platform-native format.
    /// This ensures test inputs match the platform-native line endings
    /// that Roslyn code fixers generate.
    /// </summary>
    public static string NormalizeLineEndings(this string value)
    {
        return value.Replace("\r\n", "\n").Replace("\n", Environment.NewLine);
    }
}
