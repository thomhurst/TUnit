using System;
using System.Linq;
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

        if (constructorArgument.Kind is TypedConstantKind.Enum || type?.TypeKind == TypeKind.Enum)
        {
            return $"({(type ?? constructorArgument.Type)!.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix)})({constructorArgument.Value})";
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

    private static object? ParsePrimitive(TypedConstant constructorArgument)
    {
        var type = constructorArgument.Type?.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix);

        if (type == "global::System.Boolean")
        {
            return constructorArgument.Value?.ToString()?.ToLowerInvariant();
        }
        
        if (type == "global::System.String")
        {
            return $"\"{constructorArgument.Value}\"";
        }
        
        return constructorArgument.Value;
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