using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.Extensions;

public static class EnumerableExtensions
{
    public static string ToCommaSeparatedString<T>(this IEnumerable<T> enumerable)
    {
        return string.Join(", ", enumerable);
    }

    public static IEnumerable<AttributeData> ExceptSystemAttributes(this IEnumerable<AttributeData> attributeDatas)
    {
        return attributeDatas.Where(x => x.AttributeClass?.ContainingNamespace.Name.StartsWith("System") != true);
    }
}
