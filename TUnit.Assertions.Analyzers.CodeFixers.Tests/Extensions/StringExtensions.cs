namespace TUnit.Assertions.Analyzers.CodeFixers.Tests.Extensions;

public static class StringExtensions
{
    public static string NormalizeLineEndings(this string input)
    {
        return input.Replace("\r\n", "\n");
    }
}