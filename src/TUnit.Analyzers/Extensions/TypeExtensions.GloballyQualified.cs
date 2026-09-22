using Microsoft.CodeAnalysis;

// Shared between TUnit.Analyzers and TUnit.Assertions.Analyzers (linked into the latter via <Compile Include>).
// The namespace is chosen per project so `DisplayFormats` binds to each assembly's own formats
// (which intentionally differ) and existing call sites keep working unchanged.
#if TUNIT_ASSERTIONS_ANALYZERS
namespace TUnit.Assertions.Analyzers.Extensions;
#else
namespace TUnit.Analyzers.Extensions;
#endif

public static partial class TypeExtensions
{
    public static string GloballyQualified(this ISymbol typeSymbol) =>
        typeSymbol.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix);

    public static string GloballyQualifiedNonGeneric(this ISymbol typeSymbol) =>
        typeSymbol.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix);

    /// <summary>
    /// Equivalent to <c>symbol.GloballyQualified() == expected</c>, but first rejects on the symbol's
    /// simple name so the (allocating, comparatively expensive) display string is only built for
    /// symbols that can actually match. Analyzers call this for every attribute / base type / parameter
    /// of every symbol in the compilation, so this matters on large projects.
    /// </summary>
    public static bool IsGloballyQualified(this ISymbol symbol, string expected)
        => CanHaveGloballyQualifiedName(symbol, expected) && symbol.GloballyQualified() == expected;

    /// <summary>
    /// Equivalent to <c>symbol.GloballyQualifiedNonGeneric() == expected</c>; see <see cref="IsGloballyQualified"/>.
    /// </summary>
    public static bool IsGloballyQualifiedNonGeneric(this ISymbol symbol, string expected)
        => CanHaveGloballyQualifiedName(symbol, expected) && symbol.GloballyQualifiedNonGeneric() == expected;

    // Returns false only when the display string can't possibly equal `expected`.
    private static bool CanHaveGloballyQualifiedName(ISymbol symbol, string expected)
    {
        // Named types and ordinary methods always render their Name as the last segment of the display string.
        if (symbol is not INamedTypeSymbol and not IMethodSymbol { MethodKind: MethodKind.Ordinary })
        {
            return true;
        }

        if (!TryGetLastSegmentName(expected, out var start, out var length))
        {
            return true;
        }

        var name = symbol.Name;

        return name.Length == length && string.CompareOrdinal(name, 0, expected, start, length) == 0;
    }

    // For "global::A.B<T>.C<U>" yields "C". Returns false for anything that isn't a plain
    // global-prefixed dotted identifier path with optional generic argument lists.
    private static bool TryGetLastSegmentName(string expected, out int start, out int length)
    {
        const string GlobalPrefix = "global::";

        start = 0;
        length = 0;

        if (!expected.StartsWith(GlobalPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        var depth = 0;
        var segmentStart = GlobalPrefix.Length;
        var segmentNameEnd = -1;

        for (var i = GlobalPrefix.Length; i < expected.Length; i++)
        {
            var c = expected[i];

            if (c == '<')
            {
                if (depth == 0 && segmentNameEnd < 0)
                {
                    segmentNameEnd = i;
                }

                depth++;
            }
            else if (c == '>')
            {
                if (--depth < 0)
                {
                    return false;
                }
            }
            else if (depth == 0)
            {
                if (c == '.')
                {
                    segmentStart = i + 1;
                    segmentNameEnd = -1;
                }
                else if (segmentNameEnd >= 0 || !(char.IsLetterOrDigit(c) || c == '_'))
                {
                    return false;
                }
            }
        }

        if (depth != 0)
        {
            return false;
        }

        start = segmentStart;
        length = (segmentNameEnd >= 0 ? segmentNameEnd : expected.Length) - segmentStart;

        return length > 0;
    }
}
