using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.Enums;
using TUnit.Engine.SourceGenerator.Extensions;
using TUnit.Engine.SourceGenerator.Models.Arguments;

namespace TUnit.Engine.SourceGenerator.CodeGenerators.Helpers;

internal static class MethodDataSourceRetriever
{
    public static ArgumentsContainer ParseMethodData(GeneratorAttributeSyntaxContext context,
        ImmutableArray<IParameterSymbol> parameters,
        INamedTypeSymbol namedTypeSymbol, AttributeData methodDataAttribute, ArgumentsType argumentsType, int index)
    {
        string typeName;
        IMethodSymbol? dataSourceMethod;
        if (methodDataAttribute.ConstructorArguments.Length == 1)
        {
            typeName =
                namedTypeSymbol.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix);

            dataSourceMethod = namedTypeSymbol.GetMembers(methodDataAttribute.ConstructorArguments[0].Value!.ToString())
                .OfType<IMethodSymbol>().First();
        }
        else
        {
            var typeContainingDataSourceMethod = (INamedTypeSymbol)methodDataAttribute.ConstructorArguments[0].Value!;
            
            typeName = typeContainingDataSourceMethod.ToDisplayString(DisplayFormats
                .FullyQualifiedGenericWithGlobalPrefix);

            dataSourceMethod = typeContainingDataSourceMethod
                .GetMembers(methodDataAttribute.ConstructorArguments[1].Value!.ToString())
                .OfType<IMethodSymbol>().First();
        }

        var methodName = dataSourceMethod.Name;
        var isStatic = dataSourceMethod.IsStatic;

        var disposeAfterTest =
            methodDataAttribute.NamedArguments.FirstOrDefault(x => x.Key == "DisposeAfterTest").Value.Value as bool? ??
            true;

        var isEnumerable = dataSourceMethod.ReturnType.EnumerableGenericTypeIs(context, parameters.Select(x => x.Type).ToImmutableArray(), out var innerType);
        
        if (!isEnumerable)
        {
            innerType = dataSourceMethod.ReturnType!;
        }
        
        if (innerType!.IsTupleType && innerType is INamedTypeSymbol typeSymbol)
        {
            var tupleTypes = typeSymbol.TupleUnderlyingType?.TypeArguments ??
                             typeSymbol.TypeArguments;

            if (CheckTupleTypes(parameters, tupleTypes) is {} result)
            {
                return new MethodDataSourceAttributeContainer
                {
                    TestClassTypeName = namedTypeSymbol.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix),
                    ArgumentsType = argumentsType,
                    Attribute = methodDataAttribute,
                    AttributeIndex = index,
                    IsEnumerableData = isEnumerable,
                    IsStatic = isStatic,
                    MethodName = methodName,
                    TypeName = typeName,
                    DisposeAfterTest = disposeAfterTest,
                    MethodReturnType = dataSourceMethod.ReturnType.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix),
                    TupleTypes = result.ToArray()
                };
            }
        }        
        
        return new MethodDataSourceAttributeContainer
        {
            TestClassTypeName = namedTypeSymbol.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix),
            ArgumentsType = argumentsType,
            Attribute = methodDataAttribute,
            AttributeIndex = index,
            IsEnumerableData = isEnumerable,
            IsStatic = isStatic,
            MethodName = methodName,
            TypeName = typeName,
            DisposeAfterTest = disposeAfterTest,
            MethodReturnType = dataSourceMethod.ReturnType.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix),
            TupleTypes = [],
        };
    }

    private static IEnumerable<string> CheckTupleTypes(ImmutableArray<IParameterSymbol> parameters, ImmutableArray<ITypeSymbol> tupleTypes)
    {
        for (var index = 0; index < tupleTypes.Length; index++)
        {
            var tupleType = tupleTypes.ElementAtOrDefault(index);
            var parameterType = parameters.ElementAtOrDefault(index)?.Type;

            yield return tupleType?.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix)
                ?? parameterType?.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix)!;
        }
    }
}