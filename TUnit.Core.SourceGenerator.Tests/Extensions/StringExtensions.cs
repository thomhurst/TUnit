using System.Text.RegularExpressions;

namespace TUnit.Core.SourceGenerator.Tests.Extensions;

public static class StringExtensions
{
    public static string IgnoreWhitespaceFormatting(this string value)
    {
        value = value.Replace("\t", "  ");
        
        while (value.Contains("  "))
        {
            value = value.Replace("  ", " ");
        }

        return Regex.Replace(value, "\\s+", " ");
    }
}