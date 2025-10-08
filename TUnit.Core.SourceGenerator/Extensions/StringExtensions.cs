namespace TUnit.Core.SourceGenerator.Extensions;

public static class StringExtensions
{
    public static string ReplaceFirstOccurrence(this string source, string find, string replace)
    {
        var place = source.IndexOf(find, StringComparison.Ordinal);

        return place == -1 ? source : source.Remove(place, find.Length).Insert(place, replace);
    }

    public static string ReplaceLastOccurrence(this string source, string find, string replace)
    {
        var place = source.LastIndexOf(find, StringComparison.Ordinal);

        return place == -1 ? source : source.Remove(place, find.Length).Insert(place, replace);
    }
}
