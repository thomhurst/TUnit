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

            yield return new Argument(type, null);
            yield break;
        }

        for (var index = 0; index < objectArray.Length; index++)
        {
            if (parameterOrPropertyTypeSymbols[index].IsCollectionType(context.SemanticModel.Compilation, out var innerType)
                && objectArray.Skip(index).Select(x => x.Type).All(x => SymbolEqualityComparer.Default.Equals(x, innerType)))
            {
                var paramArgs = objectArray.Skip(index)
                    .Select((x, i) => TypedConstantParser.GetTypedConstantValue(context.SemanticModel, (x, arguments.Skip(index).ElementAt(i)), x.Type));
                
                yield return
                    new Argument(parameterOrPropertyTypeSymbols[index].GloballyQualified(), $"[{string.Join(", ", paramArgs)}]");
                
                yield break;
            }
            
            var typedConstant = objectArray[index];
            var argumentAttribute = arguments[index];
            
            var type = GetTypeFromParameters(parameterOrPropertyTypeSymbols, index);
            
            yield return new Argument(type?.GloballyQualified() ??
                                TypedConstantParser.GetFullyQualifiedTypeNameFromTypedConstantValue(
                                    typedConstant),
                    
                TypedConstantParser.GetTypedConstantValue(context.SemanticModel, (typedConstant, argumentAttribute), type));
        }
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