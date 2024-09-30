using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.Extensions;
using TUnit.Engine.SourceGenerator.Models.Arguments;

namespace TUnit.Engine.SourceGenerator.CodeGenerators.Helpers;

internal static class DataSourceGeneratorRetriever
{
    public static ArgumentsContainer Parse(GeneratorAttributeSyntaxContext context,
        ImmutableArray<IParameterSymbol> parameters,
        INamedTypeSymbol namedTypeSymbol, AttributeData attributeData, string argPrefix, int index)
    {
        var arguments = ParseMethodDataArguments(context, parameters, namedTypeSymbol, attributeData, argPrefix, out var isEnumerable);

        return new ArgumentsContainer
        {
            DataAttribute = attributeData,
            DataAttributeIndex = index,
            IsEnumerableData = true,
            Arguments = [arguments]
        };
    }

    private static Argument ParseMethodDataArguments(GeneratorAttributeSyntaxContext context,
        ImmutableArray<IParameterSymbol> parameters, INamedTypeSymbol namedTypeSymbol,
        AttributeData methodDataAttribute, string argPrefix, out bool isEnumerable)
    {
        return new Argument("",
            $"methodInfo.GetCustomAttributes<{methodDataAttribute.AttributeClass!.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix)}>().Select(x => x.GenerateDataSources({string.Join(", ", parameters.Select(x => x.Type.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix)))}))");
      
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