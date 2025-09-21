using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Core.SourceGenerator.Extensions;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Formatting;

public class TypedConstantFormatter : ITypedConstantFormatter
{
    
    public string FormatForCode(TypedConstant constant, ITypeSymbol? targetType = null)
    {
        if (constant.IsNull)
        {
            return "null";
        }

        switch (constant.Kind)
        {
            case TypedConstantKind.Primitive:
                // Check for special floating-point values first using the TypedConstant's type info
                if (constant.Type?.SpecialType == SpecialType.System_Single || 
                    constant.Type?.SpecialType == SpecialType.System_Double)
                {
                    var specialValue = Helpers.SpecialFloatingPointValuesHelper.TryFormatSpecialFloatingPointValue(constant.Value);
                    if (specialValue != null)
                    {
                        return specialValue;
                    }
                }
                return FormatPrimitiveForCode(constant.Value, targetType);
                
            case TypedConstantKind.Enum:
                return FormatEnumForCode(constant, targetType);
                
            case TypedConstantKind.Type:
                var type = (ITypeSymbol)constant.Value!;
                return $"typeof({type.GloballyQualified()})";
                
            case TypedConstantKind.Array:
                return FormatArrayForCode(constant, targetType);
                
            case TypedConstantKind.Error:
                return "default";
                
            default:
                return constant.Value?.ToString() ?? "null";
        }
    }

    public string FormatForTestId(TypedConstant constant)
    {
        if (constant.IsNull)
        {
            return "null";
        }

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
                return type.GloballyQualified();
                
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
        {
            return "null";
        }

        // Handle TypedConstant values that weren't extracted
        if (value is TypedConstant tc)
        {
            return FormatForCode(tc, targetType);
        }

        return FormatPrimitiveForCode(value, targetType);
    }

    private string FormatPrimitiveForCode(object? value, ITypeSymbol? targetType)
    {
        if (value == null)
        {
            return "null";
        }

        // Handle target type conversions
        if (targetType != null)
        {
            // Enum types
            if (targetType.TypeKind == TypeKind.Enum && targetType is INamedTypeSymbol enumType)
            {
                var memberName = GetEnumMemberName(enumType, value);
                if (memberName != null)
                {
                    var enumTypeName = targetType.GloballyQualified();
                    return $"{enumTypeName}.{memberName}";
                }
                
                // Fallback to cast for non-member values
                var formattedValue = FormatPrimitive(value);
                return formattedValue != null && formattedValue.StartsWith("-") 
                    ? $"({targetType.GloballyQualified()})({formattedValue})"
                    : $"({targetType.GloballyQualified()}){formattedValue}";
            }

            // Special handling for char to numeric conversions
            if (value is char charValue)
            {
                switch (targetType.SpecialType)
                {
                    case SpecialType.System_Byte:
                        return $"(byte){(int)charValue}";
                    case SpecialType.System_SByte:
                        return $"(sbyte){(int)charValue}";
                    case SpecialType.System_Int16:
                        return $"(short){(int)charValue}";
                    case SpecialType.System_UInt16:
                        return $"(ushort){(int)charValue}";
                    case SpecialType.System_Int32:
                        return $"{(int)charValue}";
                    case SpecialType.System_UInt32:
                        return $"{(uint)charValue}u";
                    case SpecialType.System_Int64:
                        return $"{(long)charValue}L";
                    case SpecialType.System_UInt64:
                        return $"{(ulong)charValue}UL";
                    case SpecialType.System_Single:
                        return $"{(float)charValue}f";
                    case SpecialType.System_Double:
                        return $"{(double)charValue}d";
                    case SpecialType.System_Decimal:
                        return $"{(decimal)charValue}m";
                }
            }

            // Handle numeric type conversions
            switch (targetType.SpecialType)
            {
                case SpecialType.System_Byte:
                    return $"(byte){value}";
                case SpecialType.System_SByte:
                    return $"(sbyte){value}";
                case SpecialType.System_Int16:
                    return $"(short){value}";
                case SpecialType.System_UInt16:
                    return $"(ushort){value}";
                case SpecialType.System_Int32:
                    // Int32 is the default for integer literals, no cast needed unless value is not int32
                    return value is int ? value.ToString()! : $"(int){value}";
                case SpecialType.System_UInt32:
                    return $"{value}u";
                case SpecialType.System_Int64:
                    return $"{value}L";
                case SpecialType.System_UInt64:
                    return $"{value}UL";
                case SpecialType.System_Single:
                    return $"{value}f";
                case SpecialType.System_Double:
                    // Double is default for floating-point literals
                    return value.ToString()!;
                case SpecialType.System_Decimal:
                    // Handle string to decimal conversion for values that can't be expressed as literals
                    if (value is string s)
                    {
                        // Generate code that parses the string at runtime
                        // This allows for maximum precision decimal values
                        return $"decimal.Parse(\"{s}\", System.Globalization.CultureInfo.InvariantCulture)";
                    }
                    // When target is decimal but value is double/float/int, convert and format with m suffix
                    else if (value is double d)
                    {
                        // Use the full precision by formatting with sufficient digits
                        // The 'G29' format gives us the maximum precision for decimal
                        var decimalValue = (decimal)d;
                        return $"{decimalValue.ToString("G29", System.Globalization.CultureInfo.InvariantCulture)}m";
                    }
                    else if (value is float f)
                    {
                        var decimalValue = (decimal)f;
                        return $"{decimalValue.ToString("G29", System.Globalization.CultureInfo.InvariantCulture)}m";
                    }
                    else if (value is int || value is long || value is short || value is byte ||
                             value is uint || value is ulong || value is ushort || value is sbyte)
                    {
                        // For integer types, convert to decimal
                        var decimalValue = Convert.ToDecimal(value);
                        return $"{decimalValue.ToString(System.Globalization.CultureInfo.InvariantCulture)}m";
                    }
                    return $"{value}m";
            }
        }

        return FormatPrimitive(value);
    }

    private string FormatEnumForCode(TypedConstant constant, ITypeSymbol? targetType)
    {
        var enumType = targetType as INamedTypeSymbol ?? constant.Type as INamedTypeSymbol;
        if (enumType == null)
        {
            return FormatPrimitive(constant.Value);
        }

        var memberName = GetEnumMemberName(enumType, constant.Value);
        if (memberName != null)
        {
            return $"{enumType.GloballyQualified()}.{memberName}";
        }

        // Fallback to cast syntax
        var formattedValue = FormatPrimitive(constant.Value);
        return formattedValue != null && formattedValue.StartsWith("-")
            ? $"({enumType.GloballyQualified()})({formattedValue})"
            : $"({enumType.GloballyQualified()}){formattedValue}";
    }

    private string FormatArrayForCode(TypedConstant constant, ITypeSymbol? targetType = null)
    {
        // For arrays, determine the element type from the target type if available
        ITypeSymbol? elementType = null;
        if (targetType is IArrayTypeSymbol arrayTargetType)
        {
            elementType = arrayTargetType.ElementType;
        }
        else
        {
            elementType = (constant.Type as IArrayTypeSymbol)?.ElementType;
        }
        
        var elements = constant.Values.Select(v => FormatForCode(v, elementType));
        var elementTypeString = elementType?.GloballyQualified() ?? "object";
        return $"new {elementTypeString}[] {{ {string.Join(", ", elements)} }}";
    }

    private static string FormatPrimitive(object? value)
    {
        // Check for special floating-point values first
        var specialFloatValue = Helpers.SpecialFloatingPointValuesHelper.TryFormatSpecialFloatingPointValue(value);
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
            case long l:
                return l.ToString(System.Globalization.CultureInfo.InvariantCulture) + "L";
            case ulong ul:
                return ul.ToString(System.Globalization.CultureInfo.InvariantCulture) + "UL";
            case uint ui:
                return ui.ToString(System.Globalization.CultureInfo.InvariantCulture) + "U";
            default:
                // For other numeric types, use InvariantCulture
                if (value is IFormattable formattable)
                {
                    return formattable.ToString(null, System.Globalization.CultureInfo.InvariantCulture);
                }
                return value.ToString() ?? "null";
        }
    }

    private string? GetEnumMemberName(INamedTypeSymbol? enumType, object? value)
    {
        if (enumType == null || value == null)
        {
            return null;
        }

        foreach (var member in enumType.GetMembers())
        {
            if (member is IFieldSymbol { IsConst: true, HasConstantValue: true } field)
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
        {
            return enumValue == providedValue;
        }

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