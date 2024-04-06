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
            var type = (INamedTypeSymbol) constructorArgument.Value!;
            return $"typeof({type.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix)})";
        }

        if (constructorArgument.Kind == TypedConstantKind.Array)
        {
            return $"[{string.Join(",", constructorArgument.Values.Select(GetTypedConstantValue))}]";
        }

        throw new ArgumentOutOfRangeException();
    }
}