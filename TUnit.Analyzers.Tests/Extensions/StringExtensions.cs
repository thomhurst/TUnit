namespace TUnit.Analyzers.Tests.Extensions;

public static class StringExtensions
{
    public static string NormalizeLineEndings(this string value)
    {
        return value.Replace("\r\n", "\n").Replace("\n", "\r\n");
    }
}
