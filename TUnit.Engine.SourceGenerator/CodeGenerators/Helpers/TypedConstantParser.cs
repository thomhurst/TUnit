using Microsoft.CodeAnalysis;
using System.Globalization;

namespace TUnit.Engine.SourceGenerator.CodeGenerators.Helpers;

internal static class TypedConstantParser
{
    public static string? GetTypedConstantValue(TypedConstant constructorArgument, ITypeSymbol? type = null)
    {
        if (constructorArgument.IsNull)
        {
            return "null";
        }
        
        if (constructorArgument.Kind == TypedConstantKind.Error)
        {
            return null;
        }

        if (constructorArgument.Type?.SpecialType is SpecialType.System_String or SpecialType.System_Char)
        {
            return $"{ValueToString(constructorArgument.Value)?.Replace(@"\", @"\\")}";
        }

        if (constructorArgument.Kind is TypedConstantKind.Enum || type?.TypeKind == TypeKind.Enum)
        {
            return $"({(type ?? constructorArgument.Type)!.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix)})({ValueToString(constructorArgument.Value)})";
        }
        
        if (constructorArgument.Type?.SpecialType == SpecialType.System_Single)
        {
            return $"{ValueToString(constructorArgument.Value)}F";
        }
        
        if (constructorArgument.Type?.SpecialType == SpecialType.System_Int64)
        {
            return $"{ValueToString(constructorArgument.Value)}L";
        }
        
        if (constructorArgument.Type?.SpecialType == SpecialType.System_Double)
        {
            return $"{ValueToString(constructorArgument.Value)}D";
        }
        
        if (constructorArgument.Type?.SpecialType == SpecialType.System_Decimal)
        {
            return $"{ValueToString(constructorArgument.Value)}M";
        }

        if (constructorArgument.Type?.SpecialType == SpecialType.System_UInt32)
        {
            return $"{ValueToString(constructorArgument.Value)}U";
        }
        
        if (constructorArgument.Type?.SpecialType == SpecialType.System_UInt64)
        {
            return $"{ValueToString(constructorArgument.Value)}UL";
        }
        
        if (constructorArgument.Type?.SpecialType == SpecialType.System_Decimal)
        {
            return $"{ValueToString(constructorArgument.Value)}M";
        }
        
        if (constructorArgument.Kind is TypedConstantKind.Primitive)
        {
            return $"{ValueToString(constructorArgument.Value)}";
        }
        
        if (constructorArgument.Kind is TypedConstantKind.Type)
        {
            return $"typeof({GetFullyQualifiedTypeNameFromTypedConstantValue(constructorArgument)})";
        }

        if (constructorArgument.Kind == TypedConstantKind.Array)
        {
            return $"[{string.Join(",", constructorArgument.Values.Select(constructorArgument1 => GetTypedConstantValue(constructorArgument1)))}]";
        }

        throw new ArgumentOutOfRangeException();
    }

    public static string GetFullyQualifiedTypeNameFromTypedConstantValue(TypedConstant typedConstant)
    {
        if (typedConstant.Kind == TypedConstantKind.Type)
        {
            var type = (INamedTypeSymbol) typedConstant.Value!;
            return type.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix);
        }
        
        if (typedConstant.Kind == TypedConstantKind.Enum)
        {
            return typedConstant.Type!.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix);
        }

        if (typedConstant.Kind is not TypedConstantKind.Error and not TypedConstantKind.Array)
        {
            return $"global::{typedConstant.Value!.GetType().FullName}";
        }
        
        return typedConstant.Type!.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix);
    }

    private static string? ValueToString(object? value)
    {
        if (value is IFormattable formattable)
        {
            return formattable.ToString(null, CultureInfo.InvariantCulture);
        }
        return value?.ToString();
    }
}