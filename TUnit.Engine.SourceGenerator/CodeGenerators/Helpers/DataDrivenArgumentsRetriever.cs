using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using TUnit.Engine.SourceGenerator.Extensions;
using TUnit.Engine.SourceGenerator.Models.Arguments;

namespace TUnit.Engine.SourceGenerator.CodeGenerators.Helpers;

internal static class DataDrivenArgumentsRetriever
{
    public static ArgumentsContainer ParseArguments(GeneratorAttributeSyntaxContext context, AttributeData argumentAttribute, ImmutableArray<IParameterSymbol> parameterSymbols,
        int dataAttributeIndex)
    {
        var constructorArgument = argumentAttribute.ConstructorArguments.SafeFirstOrDefault();

        if (constructorArgument.IsNull)
        {
            return new ArgumentsContainer
            {
                DataAttribute = argumentAttribute,
                DataAttributeIndex = dataAttributeIndex,
                IsEnumerableData = false,
                Arguments =
                [
                    new Argument(type: parameterSymbols.SafeFirstOrDefault()?.Type
                            .ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix) ?? "var",
                        invocation: null
                    ),
                ]
            };
        }

        var attributeSyntax = (AttributeSyntax)argumentAttribute.ApplicationSyntaxReference!.GetSyntax();
        var arguments = attributeSyntax.ArgumentList!.Arguments;
        var objectArray = constructorArgument.Values;

        var args = GetArguments(context, objectArray, arguments, parameterSymbols);

        return new ArgumentsContainer
        {
            DataAttribute = argumentAttribute,
            DataAttributeIndex = dataAttributeIndex,
            IsEnumerableData = false,
            Arguments = [.. args]
        };
    }

    private static IEnumerable<Argument> GetArguments(GeneratorAttributeSyntaxContext context,
        ImmutableArray<TypedConstant> objectArray,
        SeparatedSyntaxList<AttributeArgumentSyntax> arguments,
        ImmutableArray<IParameterSymbol> parameterSymbols)
    {
        if (objectArray.IsDefaultOrEmpty)
        {
            var type = parameterSymbols.SafeFirstOrDefault()?.Type
                ?.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix) ?? "var";

            return [new Argument(type, null)];
        }

        return objectArray.Zip(arguments, (o, a) => (o, a)).Select((element, index) =>
        {
            var type = GetTypeFromParameters(parameterSymbols, index);

            return new Argument(type?.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix) ??
                                TypedConstantParser.GetFullyQualifiedTypeNameFromTypedConstantValue(element.o),
            TypedConstantParser.GetTypedConstantValue(context.SemanticModel, element.a.Expression, type));
        });
    }

    private static ITypeSymbol? GetTypeFromParameters(ImmutableArray<IParameterSymbol> parameterSymbols, int index)
    {
        if (parameterSymbols.IsDefaultOrEmpty)
        {
            return null;
        }

        return parameterSymbols.ElementAtOrDefault(index)?.Type;
    }
}