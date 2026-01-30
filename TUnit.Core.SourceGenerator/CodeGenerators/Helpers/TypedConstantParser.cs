using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Core.SourceGenerator.CodeGenerators.Formatting;
using TUnit.Core.SourceGenerator.Extensions;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Helpers;

public static class TypedConstantParser
{
    private static readonly TypedConstantFormatter _formatter = new();

    public static string GetFullyQualifiedTypeNameFromTypedConstantValue(TypedConstant typedConstant)
    {
        if (typedConstant.Kind == TypedConstantKind.Type)
        {
            var type = (INamedTypeSymbol) typedConstant.Value!;
            return type.GloballyQualified();
        }

        if (typedConstant.Kind == TypedConstantKind.Enum)
        {
            return typedConstant.Type!.GloballyQualified();
        }

        if (typedConstant.Kind is not TypedConstantKind.Error and not TypedConstantKind.Array)
        {
            return $"global::{typedConstant.Value!.GetType().FullName}";
        }

        return typedConstant.Type!.GloballyQualified();
    }

    public static string GetRawTypedConstantValue(TypedConstant typedConstant, ITypeSymbol? targetType = null)
    {
        // Use the formatter for consistent handling
        return _formatter.FormatForCode(typedConstant, targetType);
    }

    public static string FormatPrimitive(object? value)
    {
        // Check for special floating-point values first
        var specialFloatValue = SpecialFloatingPointValuesHelper.TryFormatSpecialFloatingPointValue(value);
        if (specialFloatValue != null)
        {
            return specialFloatValue;
        }

        switch (value)
        {
            case string s:
                return SymbolDisplay.FormatLiteral(s, quote: true);
            case char c:
                return SymbolDisplay.FormatLiteral(c, quote: true);
            case bool b:
                return b ? "true" : "false";
            case null:
                return "null";
            // Use InvariantCulture for numeric types to ensure consistent formatting
            case double d:
                return d.ToString(System.Globalization.CultureInfo.InvariantCulture) + "d";
            case float f:
                return f.ToString(System.Globalization.CultureInfo.InvariantCulture) + "f";
            case decimal dec:
                return dec.ToString(System.Globalization.CultureInfo.InvariantCulture) + "m";
            default:
                // For other numeric types, use InvariantCulture
                if (value is IFormattable formattable)
                {
                    return formattable.ToString(null, System.Globalization.CultureInfo.InvariantCulture);
                }
                return value.ToString() ?? "null";
        }
    }
}
