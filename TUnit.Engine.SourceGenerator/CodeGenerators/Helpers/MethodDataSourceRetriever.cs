using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.Extensions;
using TUnit.Engine.SourceGenerator.Models.Arguments;

namespace TUnit.Engine.SourceGenerator.CodeGenerators.Helpers;

internal static class MethodDataSourceRetriever
{
    public static ArgumentsContainer ParseMethodData(GeneratorAttributeSyntaxContext context,
        ImmutableArray<IParameterSymbol> parameters,
        INamedTypeSymbol namedTypeSymbol, AttributeData attributeData, string argPrefix, int index)
    {
        var arguments = ParseMethodDataArguments(context, parameters, namedTypeSymbol, attributeData, argPrefix, out var isEnumerable);
        
        return new ArgumentsContainer
        {
            DataAttribute = attributeData,
            DataAttributeIndex = index,
            IsEnumerableData = isEnumerable,
            Arguments = [arguments]
        };
    }

    private static Argument ParseMethodDataArguments(GeneratorAttributeSyntaxContext context,
        ImmutableArray<IParameterSymbol> parameters, INamedTypeSymbol namedTypeSymbol,
        AttributeData methodDataAttribute, string argPrefix, out bool isEnumerable)
    {
        string methodInvocation;
        IMethodSymbol? dataSourceMethod;
        if (methodDataAttribute.ConstructorArguments.Length == 1)
        {
            var typeContainingMethod =
                namedTypeSymbol.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix);

            dataSourceMethod = namedTypeSymbol.GetMembers(methodDataAttribute.ConstructorArguments[0].Value!.ToString())
                .OfType<IMethodSymbol>().First();

            methodInvocation = dataSourceMethod.IsStatic
                ? $"{typeContainingMethod}.{methodDataAttribute.ConstructorArguments.SafeFirstOrDefault().Value!}()"
                : $"resettableClassFactory.Value.{methodDataAttribute.ConstructorArguments.SafeFirstOrDefault().Value!}()";
        }
        else
        {
            var typeContainingDataSourceMethod = (INamedTypeSymbol)methodDataAttribute.ConstructorArguments[0].Value!;
            var type = typeContainingDataSourceMethod.ToDisplayString(DisplayFormats
                .FullyQualifiedGenericWithGlobalPrefix);

            methodInvocation = $"{type}.{methodDataAttribute.ConstructorArguments[1].Value!}()";

            dataSourceMethod = typeContainingDataSourceMethod
                .GetMembers(methodDataAttribute.ConstructorArguments[1].Value!.ToString())
                .OfType<IMethodSymbol>().First();
        }

        var disposeAfterTest =
            methodDataAttribute.NamedArguments.FirstOrDefault(x => x.Key == "DisposeAfterTest").Value.Value as bool? ??
            true;

        isEnumerable = dataSourceMethod.ReturnType.EnumerableGenericTypeIs(context, parameters.Select(x => x.Type).ToImmutableArray(), out var innerType);
        
        if (!isEnumerable)
        {
            innerType = dataSourceMethod.ReturnType!;
        }
        
        if (innerType!.IsTupleType && innerType is INamedTypeSymbol typeSymbol)
        {
            var tupleTypes = typeSymbol.TupleUnderlyingType?.TypeArguments ??
                             typeSymbol.TypeArguments;

            if (CheckTupleTypes(context, parameters, dataSourceMethod, tupleTypes, methodInvocation, argPrefix,
                    disposeAfterTest) is {} result)
            {
                return result;
            }
        }

        return new Argument(dataSourceMethod.ReturnType.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix), methodInvocation)
        {
            DisposeAfterTest = disposeAfterTest
        };
    }

    private static Argument? CheckTupleTypes(GeneratorAttributeSyntaxContext context,
        ImmutableArray<IParameterSymbol> parameters,
        IMethodSymbol dataSourceMethod, ImmutableArray<ITypeSymbol> tupleTypes, string methodInvocation, string argPrefix,
        bool disposeAfterTest)
    {
        for (var index = 0; index < tupleTypes.Length; index++)
        {
            var tupleType = tupleTypes.ElementAtOrDefault(index);
            var parameterType = parameters.ElementAtOrDefault(index)?.Type;

            if (!context.SemanticModel.Compilation.HasImplicitConversion(tupleType, parameterType))
            {
                return null;
            }
        }
        
        var variableNames = parameters.Select((_, i) => $"{argPrefix}{i}").ToArray();

        return new Argument(
            dataSourceMethod.ReturnType.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix),
            methodInvocation)
        {
            TupleVariableNames = variableNames,
            DisposeAfterTest = disposeAfterTest
        };
    }
}