using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Core.SourceGenerator.Extensions;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Helpers;

internal static class CastExpressionHelper
{
    /// <summary>
    /// Dispatches to the appropriate cast generation strategy based on the source type info at the given position.
    /// When <paramref name="fallbackSourceType"/> is provided, it is used as the source type for unknown positions
    /// instead of falling back to CastHelper.Cast (useful for params elements where the element type is statically known).
    /// </summary>
    public static string GenerateCastForPosition(
        SourceTypeInfo? sourceTypeInfo,
        int position,
        ITypeSymbol targetType,
        string argsExpression,
        CSharpCompilation? compilation,
        ITypeSymbol? fallbackSourceType = null)
    {
        if (sourceTypeInfo != null)
        {
            var types = sourceTypeInfo.GetTypes(position);

            if (types != null)
            {
                if (types.Count == 1)
                {
                    return GenerateCast(types[0], targetType, argsExpression, compilation);
                }

                return GenerateMultiSourceCast(types, targetType, argsExpression, compilation);
            }
        }

        // Unknown type at this position
        return GenerateCast(fallbackSourceType, targetType, argsExpression, compilation);
    }

    /// <summary>
    /// Generates a pattern-matching switch expression for positions with multiple known source types.
    /// Each source type gets its own pattern match arm with the appropriate cast.
    /// The default arm handles any unexpected types via direct cast.
    /// </summary>
    public static string GenerateMultiSourceCast(
        IReadOnlyList<ITypeSymbol> sourceTypes,
        ITypeSymbol targetType,
        string argsExpression,
        CSharpCompilation? compilation)
    {
        var targetGQ = targetType.GloballyQualified();
        var arms = new List<string>();

        // Sort source types so that derived types come before their base types.
        // Without this, a base type pattern arm would catch derived instances first,
        // causing CS8510 "unreachable pattern" errors.
        var sorted = SortMostDerivedFirst(sourceTypes);

        // In switch arms the pattern match performs the unbox, so only a single cast
        // to the target type is needed (unlike GenerateCast which needs a double-cast
        // (TargetType)(SourceType)expr to first unbox then convert).
        foreach (var sourceType in sorted)
        {
            var sourceGQ = sourceType.GloballyQualified();

            if (SymbolEqualityComparer.Default.Equals(sourceType, targetType))
            {
                arms.Add($"{sourceGQ} __s => __s");
            }
            else if (compilation != null && compilation.ClassifyConversion(sourceType, targetType) is { IsImplicit: true } or { IsExplicit: true })
            {
                arms.Add($"{sourceGQ} __s => ({targetGQ})__s");
            }
            else
            {
                arms.Add($"{sourceGQ} __s => global::TUnit.Core.Helpers.CastHelper.Cast<{targetGQ}>(__s)");
            }
        }

        // Default arm handles unboxing/direct cast from Arguments or unexpected types
        arms.Add($"_ => ({targetGQ}){argsExpression}");

        return $"({argsExpression} switch {{ {string.Join(", ", arms)} }})";
    }

    private static List<ITypeSymbol> SortMostDerivedFirst(IReadOnlyList<ITypeSymbol> types)
    {
        var result = new List<ITypeSymbol>(types);

        result.Sort((a, b) =>
        {
            if (SymbolEqualityComparer.Default.Equals(a, b))
            {
                return 0;
            }

            if (IsBaseTypeOf(a, b))
            {
                return 1;
            }

            if (IsBaseTypeOf(b, a))
            {
                return -1;
            }

            return 0;
        });

        return result;
    }

    private static bool IsBaseTypeOf(ITypeSymbol potentialBase, ITypeSymbol derived)
    {
        var current = derived.BaseType;

        while (current != null)
        {
            if (SymbolEqualityComparer.Default.Equals(current, potentialBase))
            {
                return true;
            }

            current = current.BaseType;
        }

        return false;
    }

    /// <summary>
    /// Generates a typed cast expression, or falls back to CastHelper.Cast when source type is unknown.
    /// </summary>
    public static string GenerateCast(
        ITypeSymbol? sourceType,
        ITypeSymbol targetType,
        string argsExpression,
        CSharpCompilation? compilation)
    {
        var targetGQ = targetType.GloballyQualified();

        // Unknown source or no compilation → CastHelper fallback
        if (sourceType == null || compilation == null)
        {
            return $"global::TUnit.Core.Helpers.CastHelper.Cast<{targetGQ}>({argsExpression})";
        }

        // Same type → simple cast (unbox for value types, reference cast for reference types)
        if (SymbolEqualityComparer.Default.Equals(sourceType, targetType))
        {
            return $"({targetGQ}){argsExpression}";
        }

        // Check if compiler can resolve conversion
        var conversion = compilation.ClassifyConversion(sourceType, targetType);

        // Boxing to object/ValueType/Enum: args[i] is already typed correctly, no cast needed.
        // Do NOT skip the cast for boxing-to-interface (e.g. int → IComparable) —
        // args[i] is object?, which cannot be implicitly converted to an interface type.
        if (conversion.IsBoxing
            && targetType.SpecialType is SpecialType.System_Object
                                      or SpecialType.System_ValueType
                                      or SpecialType.System_Enum)
        {
            return argsExpression;
        }

        if (conversion.IsImplicit || conversion.IsExplicit)
        {
            var sourceGQ = sourceType.GloballyQualified();
            return $"({targetGQ})({sourceGQ}){argsExpression}";
        }

        // No known conversion → CastHelper fallback
        return $"global::TUnit.Core.Helpers.CastHelper.Cast<{targetGQ}>({argsExpression})";
    }
}
