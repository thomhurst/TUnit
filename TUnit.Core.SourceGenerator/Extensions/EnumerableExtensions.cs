namespace TUnit.Core.SourceGenerator;

public static class EnumerableExtensions
{
    public static string ToCommaSeparatedString<T>(this IEnumerable<T> enumerable)
    {
        return string.Join(", ", enumerable);
    }
}