using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Core.SourceGenerator.Extensions;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Helpers;

internal static class CastExpressionHelper
{
    public static ITypeSymbol? GetSourceTypeAt(ITypeSymbol?[]? sourceTypes, int index)
    {
        return sourceTypes != null && index < sourceTypes.Length ? sourceTypes[index] : null;
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
