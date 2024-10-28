namespace TUnit.Core.SourceGenerator.Extensions;

public static class EnumerableExtensions
{
    public static string ToCommaSeparatedString<T>(this IEnumerable<T> enumerable)
    {
        return string.Join(", ", enumerable);
    }
}