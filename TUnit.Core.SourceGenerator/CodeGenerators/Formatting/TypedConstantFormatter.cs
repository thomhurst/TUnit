using System.Globalization;
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
            // If we have a nullable enum target type, cast null to that type
            if (targetType?.IsNullableValueType() == true)
            {
                var underlyingType = targetType.GetNullableUnderlyingType();
                if (underlyingType?.TypeKind == TypeKind.Enum)
                {
                    // For nullable enums, we need to cast null to the nullable enum type
                    return $"({targetType.GloballyQualified()})null";
                }
            }
            return "null";
        }

        switch (constant.Kind)
        {
            case TypedConstantKind.Primitive:
                // Check for special floating-point values first using the TypedConstant's type info
                if (constant.Type?.SpecialType is SpecialType.System_Single or SpecialType.System_Double)
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
                    return $"(byte){value.ToInvariantString()}";
                case SpecialType.System_SByte:
                    return $"(sbyte){value.ToInvariantString()}";
                case SpecialType.System_Int16:
                    return $"(short){value.ToInvariantString()}";
                case SpecialType.System_UInt16:
                    return $"(ushort){value.ToInvariantString()}";
                case SpecialType.System_Int32:
                    return value is int ? value.ToString()! : $"(int){value}";
                case SpecialType.System_UInt32:
                    return $"{value.ToInvariantString()}u";
                case SpecialType.System_Int64:
                    return $"{value.ToInvariantString()}L";
                case SpecialType.System_UInt64:
                    return $"{value.ToInvariantString()}UL";
                case SpecialType.System_Single:
                    if (value is float fl)
                    {
                        return $"{fl.ToString("G9", CultureInfo.InvariantCulture)}f";
                    }

                    return $"{value.ToInvariantString()}f";
                case SpecialType.System_Double:
                    if (value is double dbl)
                    {
                        return $"{dbl.ToString("G17", CultureInfo.InvariantCulture)}d";
                    }

                    return $"{value.ToInvariantString()}d";
                case SpecialType.System_Decimal:
                    if (value is decimal dec)
                    {
                        return $"{dec.ToString("G29", CultureInfo.InvariantCulture)}m";
                    }
                    if (value is string s)
                    {
                        return $"global::TUnit.Core.Helpers.DecimalParsingHelper.ParseDecimalWithCultureFallback(\"{s.ToInvariantString()}\")";
                    }
                    if (value is double d)
                    {
                        return $"{d.ToString("G17", CultureInfo.InvariantCulture)}m";
                    }
                    if (value is float f)
                    {
                        return $"{f.ToString("G9", CultureInfo.InvariantCulture)}m";
                    }

                    if (value is int or long or short or byte or uint or ulong or ushort or sbyte)
                    {
                        // For integer types, convert to decimal
                        var decimalValue = Convert.ToDecimal(value);
                        return $"{decimalValue.ToString("G29", CultureInfo.InvariantCulture)}m";
                    }

                    return $"{value.ToInvariantString()}m";
            }
        }

        return FormatPrimitive(value);
    }

    private string FormatEnumForCode(TypedConstant constant, ITypeSymbol? targetType)
    {
        // Check if target type is a nullable enum, and if so, get the underlying enum type
        var isNullableEnum = targetType?.IsNullableValueType() == true;
        INamedTypeSymbol? enumType = null;
        
        if (isNullableEnum)
        {
            var underlyingType = targetType!.GetNullableUnderlyingType();
            enumType = underlyingType as INamedTypeSymbol;
        }
        else
        {
            enumType = targetType as INamedTypeSymbol ?? constant.Type as INamedTypeSymbol;
        }
        
        if (enumType == null)
        {
            return FormatPrimitive(constant.Value);
        }

        var memberName = GetEnumMemberName(enumType, constant.Value);
        if (memberName != null)
        {
            var formattedEnum = $"{enumType.GloballyQualified()}.{memberName}";
            // If the target type is nullable, cast the enum value to the nullable type
            if (isNullableEnum)
            {
                return $"({targetType!.GloballyQualified()}){formattedEnum}";
            }
            return formattedEnum;
        }

        // Fallback to cast syntax
        var formattedValue = FormatPrimitive(constant.Value);
        var result = formattedValue != null && formattedValue.StartsWith("-")
            ? $"({enumType.GloballyQualified()})({formattedValue})"
            : $"({enumType.GloballyQualified()}){formattedValue}";
            
        // If the target type is nullable, wrap the result in a cast to the nullable type
        if (isNullableEnum)
        {
            return $"({targetType!.GloballyQualified()})({result})";
        }
        
        return result;
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
                return d.ToString("G17", CultureInfo.InvariantCulture) + "d";
            case float f:
                return f.ToString("G9", CultureInfo.InvariantCulture) + "f";
            case decimal dec:
                return dec.ToString("G29", CultureInfo.InvariantCulture) + "m";
            case long l:
                return l.ToString(CultureInfo.InvariantCulture) + "L";
            case ulong ul:
                return ul.ToString(CultureInfo.InvariantCulture) + "UL";
            case uint ui:
                return ui.ToString(CultureInfo.InvariantCulture) + "U";
            case byte b:
                return $"(byte){b.ToInvariantString()}";
            case sbyte b:
                return $"(sbyte){b.ToInvariantString()}";
            case ushort us:
                return $"(ushort){us.ToInvariantString()}";
            case short s:
                return $"(short){s.ToInvariantString()}";
            default:
                // For other numeric types, use InvariantCulture
                if (value is IFormattable formattable)
                {
                    return formattable.ToString(null, CultureInfo.InvariantCulture);
                }
                // For non-IFormattable types, fallback to ToString()
                // This should be safe as we've handled all numeric types above
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
            var enumLong = Convert.ToInt64(enumValue);
            var providedLong = Convert.ToInt64(providedValue);
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
