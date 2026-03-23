using Microsoft.CodeAnalysis;

namespace TUnit.Analyzers.Extensions;

internal static class ParsableTypeExtensions
{
    public static bool IsParsableFromString(this ITypeSymbol? type)
    {
        if (type is null)
        {
            return false;
        }

        if (type.AllInterfaces.Any(i =>
                i is { IsGenericType: true, MetadataName: "IParsable`1" }
                && i.ContainingNamespace?.ToDisplayString() == "System"
                && SymbolEqualityComparer.Default.Equals(i.TypeArguments[0], type)))
        {
            return true;
        }

        // Fallback for older TFMs where IParsable doesn't exist
        if (type.SpecialType == SpecialType.System_DateTime)
        {
            return true;
        }

        var fullyQualifiedName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        return fullyQualifiedName is
            "global::System.DateTimeOffset" or
            "global::System.TimeSpan" or
            "global::System.Guid" or
            "global::System.DateOnly" or
            "global::System.TimeOnly";
    }
}
