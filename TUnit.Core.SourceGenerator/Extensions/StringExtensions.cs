using System.Text.RegularExpressions;

namespace TUnit.Core.SourceGenerator.Extensions;

public static class StringExtensions
{
    public static string ReplaceFirstOccurrence(this string source, string find, string replace)
    {
        var regex = new Regex(Regex.Escape(find));
        return regex.Replace(source, replace, 1);
    }
    
    public static string ReplaceLastOccurrence(this string source, string find, string replace)
    {
        var place = source.LastIndexOf(find, StringComparison.Ordinal);
    
        return place == -1 ? source : source.Remove(place, find.Length).Insert(place, replace);
    }
}