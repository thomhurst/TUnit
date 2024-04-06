using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace TUnit.Engine.SourceGenerator.CodeGenerators;

internal static class TypedConstantParser
{
    public static string GetTypedConstantValue(TypedConstant constructorArgument)
    {
        if (constructorArgument.Kind == TypedConstantKind.Error)
        {
            return "null";
        }

        if (constructorArgument.Kind is TypedConstantKind.Primitive 
            or TypedConstantKind.Enum)
        {
            return $"{constructorArgument.Value}";
        }

        if (constructorArgument.Kind is TypedConstantKind.Type)
        {
            return $"typeof({GetFullyQualifiedTypeNameFromTypedConstantValue(constructorArgument)})";
        }

        if (constructorArgument.Kind == TypedConstantKind.Array)
        {
            return $"[{string.Join(",", constructorArgument.Values.Select(GetTypedConstantValue))}]";
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
        
        return typedConstant.Type!.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix);
    }
}