namespace TUnit.Assertions.Analyzers.CodeFixers.Tests.Extensions;

public static class StringExtensions
{
    public static string NormalizeLineEndings(this string value)
    {
        return value.Replace("\r\n", "\n").Replace("\n", Environment.NewLine);
    }
}