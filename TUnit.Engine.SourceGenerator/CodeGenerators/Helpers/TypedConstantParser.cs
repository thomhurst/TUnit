using Microsoft.CodeAnalysis;

namespace TUnit.Engine.SourceGenerator.CodeGenerators.Helpers;

internal static class TypedConstantParser
{
    public static string? GetTypedConstantValue(TypedConstant constructorArgument, ITypeSymbol? type = null)
    {
        if (constructorArgument.Kind == TypedConstantKind.Error)
        {
            return null;
        }

        if (constructorArgument.Type?.SpecialType is SpecialType.System_String or SpecialType.System_Char)
        {
            return $"{constructorArgument.Value?.ToString()?.Replace(@"\", @"\\")}";
        }

        if (constructorArgument.Kind is TypedConstantKind.Enum || type?.TypeKind == TypeKind.Enum)
        {
            return $"({(type ?? constructorArgument.Type)!.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix)})({constructorArgument.Value})";
        }
        
        if (constructorArgument.Type?.SpecialType == SpecialType.System_Single)
        {
            return $"{constructorArgument.Value}F";
        }
        
        if (constructorArgument.Type?.SpecialType == SpecialType.System_Int64)
        {
            return $"{constructorArgument.Value}L";
        }
        
        if (constructorArgument.Type?.SpecialType == SpecialType.System_Double)
        {
            return $"{constructorArgument.Value}D";
        }
        
        if (constructorArgument.Type?.SpecialType == SpecialType.System_Decimal)
        {
            return $"{constructorArgument.Value}M";
        }

        if (constructorArgument.Type?.SpecialType == SpecialType.System_UInt32)
        {
            return $"{constructorArgument.Value}U";
        }
        
        if (constructorArgument.Type?.SpecialType == SpecialType.System_UInt64)
        {
            return $"{constructorArgument.Value}UL";
        }
        
        if (constructorArgument.Type?.SpecialType == SpecialType.System_Decimal)
        {
            return $"{constructorArgument.Value}M";
        }
        
        if (constructorArgument.Kind is TypedConstantKind.Primitive)
        {
            return $"{constructorArgument.Value}";
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
}