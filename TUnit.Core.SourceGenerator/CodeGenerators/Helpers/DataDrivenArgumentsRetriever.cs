using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using TUnit.Core.SourceGenerator.Enums;
using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator.Models.Arguments;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Helpers;

public static class DataDrivenArgumentsRetriever
{
    public static ArgumentsContainer ParseArguments(GeneratorAttributeSyntaxContext context,
        AttributeData argumentAttribute, 
        ImmutableArray<ITypeSymbol> parameterOrPropertyTypeSymbols,
        ArgumentsType argumentsType,
        int dataAttributeIndex)
    {
        var constructorArgument = argumentAttribute.ConstructorArguments.SafeFirstOrDefault();

        if (constructorArgument.IsNull)
        {
            return new ArgumentsAttributeContainer(argumentsType, [new Argument(type: parameterOrPropertyTypeSymbols.SafeFirstOrDefault()?
                    .GloballyQualified() ?? "var",
                invocation: null
            )])
            {
                ArgumentsType = argumentsType,
                Attribute = argumentAttribute,
                AttributeIndex = dataAttributeIndex,
                DisposeAfterTest = argumentAttribute.NamedArguments.FirstOrDefault(x => x.Key == "DisposeAfterTest").Value.Value as bool? ?? true,
            };
        }

        var attributeSyntax = (AttributeSyntax)argumentAttribute.ApplicationSyntaxReference!.GetSyntax();
        var arguments = attributeSyntax.ArgumentList!.Arguments;
        var objectArray = constructorArgument.Values;

        var args = GetArguments(context, objectArray, arguments, parameterOrPropertyTypeSymbols);

        return new ArgumentsAttributeContainer(argumentsType, [.. args])
        {
            Attribute = argumentAttribute,
            AttributeIndex = dataAttributeIndex,
            DisposeAfterTest = argumentAttribute.NamedArguments.FirstOrDefault(x => x.Key == "DisposeAfterTest").Value.Value as bool? ?? true,
        };
    }

    private static IEnumerable<Argument> GetArguments(GeneratorAttributeSyntaxContext context,
        ImmutableArray<TypedConstant> objectArray,
        SeparatedSyntaxList<AttributeArgumentSyntax> arguments,
        ImmutableArray<ITypeSymbol> parameterOrPropertyTypeSymbols)
    {
        if (objectArray.IsDefaultOrEmpty)
        {
            var type = parameterOrPropertyTypeSymbols.SafeFirstOrDefault()
                ?.GloballyQualified() ?? "var";

            return [new Argument(type, null)];
        }

        if (parameterOrPropertyTypeSymbols is { Length: 1 }
            && parameterOrPropertyTypeSymbols[0].IsCollectionType(context.SemanticModel.Compilation, out var innerType)
            && objectArray.Select(x => x.Type).All(x => SymbolEqualityComparer.Default.Equals(x, innerType)))
        {
            return
            [
                new Argument(parameterOrPropertyTypeSymbols[0].GloballyQualified(),
                    $"[{string.Join(", ", objectArray.Select((x, i) => TypedConstantParser.GetTypedConstantValue(context.SemanticModel, (x, arguments[i]), x.Type)))}]")
            ];
        }

        return objectArray.Zip(arguments, (typedConstant, a) => (typedConstant, a))
            .Select((element, index) =>
            {
                var type = GetTypeFromParameters(parameterOrPropertyTypeSymbols, index);

                return new Argument(type?.GloballyQualified() ??
                                    TypedConstantParser.GetFullyQualifiedTypeNameFromTypedConstantValue(
                                        element.typedConstant),
                    
                    TypedConstantParser.GetTypedConstantValue(context.SemanticModel, element, type));
            });
    }

    private static ITypeSymbol? GetTypeFromParameters(ImmutableArray<ITypeSymbol> parameterSymbols, int index)
    {
        if (parameterSymbols.IsDefaultOrEmpty)
        {
            return null;
        }

        return parameterSymbols.ElementAtOrDefault(index);
    }
}