using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using TUnit.Core.SourceGenerator.Extensions;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Formatting;

public class TypedConstantFormatter : ITypedConstantFormatter
{
    private static readonly SymbolDisplayFormat FullyQualifiedFormat = SymbolDisplayFormat.FullyQualifiedFormat;
    
    public string FormatForCode(TypedConstant constant, ITypeSymbol? targetType = null)
    {
        if (constant.IsNull)
            return "null";

        switch (constant.Kind)
        {
            case TypedConstantKind.Primitive:
                return FormatPrimitiveForCode(constant.Value, targetType);
                
            case TypedConstantKind.Enum:
                return FormatEnumForCode(constant, targetType);
                
            case TypedConstantKind.Type:
                var type = (ITypeSymbol)constant.Value!;
                return $"typeof({type.ToDisplayString(FullyQualifiedFormat)})";
                
            case TypedConstantKind.Array:
                return FormatArrayForCode(constant);
                
            case TypedConstantKind.Error:
                return "default";
                
            default:
                return constant.Value?.ToString() ?? "null";
        }
    }

    public string FormatForTestId(TypedConstant constant)
    {
        if (constant.IsNull)
            return "null";

        switch (constant.Kind)
        {
            case TypedConstantKind.Primitive:
                return EscapeForTestId(constant.Value?.ToString() ?? "null");
                
            case TypedConstantKind.Enum:
                // For test IDs, use the numeric value or member name
                var enumType = constant.Type as INamedTypeSymbol;
                var memberName = GetEnumMemberName(enumType, constant.Value);
                return memberName ?? constant.Value?.ToString() ?? "null";
                
            case TypedConstantKind.Type:
                var type = (ITypeSymbol)constant.Value!;
                return type.ToDisplayString(FullyQualifiedFormat);
                
            case TypedConstantKind.Array:
                var elements = constant.Values.Select(v => FormatForTestId(v));
                return $"[{string.Join(", ", elements)}]";
                
            default:
                return EscapeForTestId(constant.Value?.ToString() ?? "null");
        }
    }

    public string FormatValue(object? value, ITypeSymbol? targetType = null)
    {
        if (value == null)
            return "null";

        // Handle TypedConstant values that weren't extracted
        if (value is TypedConstant tc)
            return FormatForCode(tc, targetType);

        return FormatPrimitiveForCode(value, targetType);
    }

    private string FormatPrimitiveForCode(object? value, ITypeSymbol? targetType)
    {
        if (value == null)
            return "null";

        // Handle target type conversions
        if (targetType != null)
        {
            // Enum types
            if (targetType.TypeKind == TypeKind.Enum && targetType is INamedTypeSymbol enumType)
            {
                var memberName = GetEnumMemberName(enumType, value);
                if (memberName != null)
                {
                    var enumTypeName = targetType.ToDisplayString(FullyQualifiedFormat);
                    return $"{enumTypeName}.{memberName}";
                }
                
                // Fallback to cast for non-member values
                var formattedValue = FormatPrimitive(value);
                return formattedValue != null && formattedValue.StartsWith("-") 
                    ? $"({targetType.ToDisplayString(FullyQualifiedFormat)})({formattedValue})"
                    : $"({targetType.ToDisplayString(FullyQualifiedFormat)}){formattedValue}";
            }

            // Float types
            if (targetType.SpecialType == SpecialType.System_Single)
            {
                return $"{value}f";
            }

            // Decimal types
            if (targetType.SpecialType == SpecialType.System_Decimal)
            {
                return $"{value}m";
            }
        }

        return FormatPrimitive(value);
    }

    private string FormatEnumForCode(TypedConstant constant, ITypeSymbol? targetType)
    {
        var enumType = (targetType as INamedTypeSymbol) ?? (constant.Type as INamedTypeSymbol);
        if (enumType == null)
            return FormatPrimitive(constant.Value);

        var memberName = GetEnumMemberName(enumType, constant.Value);
        if (memberName != null)
        {
            return $"{enumType.ToDisplayString(FullyQualifiedFormat)}.{memberName}";
        }

        // Fallback to cast syntax
        var formattedValue = FormatPrimitive(constant.Value);
        return formattedValue != null && formattedValue.StartsWith("-")
            ? $"({enumType.ToDisplayString(FullyQualifiedFormat)})({formattedValue})"
            : $"({enumType.ToDisplayString(FullyQualifiedFormat)}){formattedValue}";
    }

    private string FormatArrayForCode(TypedConstant constant)
    {
        var elements = constant.Values.Select(v => FormatForCode(v));
        var elementType = (constant.Type as IArrayTypeSymbol)?.ElementType.ToDisplayString(FullyQualifiedFormat) ?? "object";
        return $"new {elementType}[] {{ {string.Join(", ", elements)} }}";
    }

    private static string FormatPrimitive(object? value)
    {
        switch (value)
        {
            case string s:
                return SymbolDisplay.FormatLiteral(s, quote: true);
            case char c:
                return SymbolDisplay.FormatLiteral(c, quote: true);
            case bool b:
                return b ? "true" : "false";
            case float.NaN:
                return "float.NaN";
            case float f when float.IsPositiveInfinity(f):
                return "float.PositiveInfinity";
            case float f when float.IsNegativeInfinity(f):
                return "float.NegativeInfinity";
            case double.NaN:
                return "double.NaN";
            case double d when double.IsPositiveInfinity(d):
                return "double.PositiveInfinity";
            case double d when double.IsNegativeInfinity(d):
                return "double.NegativeInfinity";
            case null:
                return "null";
            default:
                return value.ToString() ?? "null";
        }
    }

    private string? GetEnumMemberName(INamedTypeSymbol? enumType, object? value)
    {
        if (enumType == null || value == null)
            return null;

        foreach (var member in enumType.GetMembers())
        {
            if (member is IFieldSymbol field && field.IsConst && field.HasConstantValue)
            {
                if (AreValuesEqual(field.ConstantValue, value))
                {
                    return field.Name;
                }
            }
        }

        return null;
    }

    private bool AreValuesEqual(object? enumValue, object? providedValue)
    {
        if (enumValue == null || providedValue == null)
            return enumValue == providedValue;

        try
        {
            var enumLong = System.Convert.ToInt64(enumValue);
            var providedLong = System.Convert.ToInt64(providedValue);
            return enumLong == providedLong;
        }
        catch
        {
            return enumValue.Equals(providedValue);
        }
    }

    private static string EscapeForTestId(string str)
    {
        return str.Replace("\\", "\\\\")
                  .Replace("\r", "\\r")
                  .Replace("\n", "\\n")
                  .Replace("\t", "\\t")
                  .Replace("\"", "\\\"");
    }
}