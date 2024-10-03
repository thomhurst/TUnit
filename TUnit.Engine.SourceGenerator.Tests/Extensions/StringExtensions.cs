using System.Text.RegularExpressions;

namespace TUnit.Engine.SourceGenerator.Tests.Extensions;

public static partial class StringExtensions
{
    public static string IgnoreWhitespaceFormatting(this string value)
    {
        return WhitespaceRegex().Replace(value, " ");
    }

    [GeneratedRegex(@"\s{2,}", RegexOptions.Compiled)]
    private static partial Regex WhitespaceRegex();
}