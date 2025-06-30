using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Analyzers.Extensions;
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
            var typeSymbol = parameterOrPropertyTypeSymbols.SafeFirstOrDefault();

            return new ArgumentsAttributeContainer(argumentsType,
                [new Argument(type: typeSymbol.GloballyQualifiedOrFallback(), invocation: null)])
            {
                ArgumentsType = argumentsType,
                Attribute = argumentAttribute,
                AttributeIndex = dataAttributeIndex,
                DisposeAfterTest =
                    argumentAttribute.NamedArguments.FirstOrDefault(x => x.Key == "DisposeAfterTest").Value
                        .Value as bool? ?? true,
            };
        }

        var attributeSyntax = argumentAttribute.ApplicationSyntaxReference?.GetSyntax() as AttributeSyntax;

        if (attributeSyntax is null)
        {
            var fallbackArgs = GetArgumentsFallback(constructorArgument.Values, parameterOrPropertyTypeSymbols);
            return new ArgumentsAttributeContainer(argumentsType, [.. fallbackArgs])
            {
                Attribute = argumentAttribute,
                AttributeIndex = dataAttributeIndex,
                DisposeAfterTest = argumentAttribute.NamedArguments.FirstOrDefault(x => x.Key == "DisposeAfterTest").Value.Value as bool? ?? true,
            };
        }

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
            yield return new Argument(parameterOrPropertyTypeSymbols.SafeFirstOrDefault().GloballyQualifiedOrFallback(objectArray.SafeFirstOrDefault()), null);
            yield break;
        }

        for (var index = 0; index < objectArray.Length; index++)
        {
            if (parameterOrPropertyTypeSymbols[index].IsCollectionType(context.SemanticModel.Compilation, out var innerType)
                && objectArray.Skip(index).Select(x => x.Type).All(x => CanConvert(context, x, innerType)))
            {
                var paramArgs = objectArray.Skip(index)
                    .Select((x, i) => TypedConstantParser.GetTypedConstantValue(context.SemanticModel, (x, arguments.Skip(index).ElementAt(i)), x.Type));

                var globallyQualified = parameterOrPropertyTypeSymbols[index].GloballyQualifiedOrFallback(objectArray.Skip(index).SafeFirstOrDefault());

                yield return
                    new Argument(globallyQualified, $"[{string.Join(", ", paramArgs)}]");

                yield break;
            }

            var typedConstant = objectArray[index];
            var argumentAttribute = arguments[index];

            var type = GetTypeFromParameters(parameterOrPropertyTypeSymbols, index);

            yield return new Argument(type.GloballyQualifiedOrFallback(typedConstant),

                TypedConstantParser.GetTypedConstantValue(context.SemanticModel, (typedConstant, argumentAttribute), type));
        }
    }

    private static IEnumerable<Argument> GetArgumentsFallback(ImmutableArray<TypedConstant> objectArray,
        ImmutableArray<ITypeSymbol> parameterOrPropertyTypeSymbols)
    {
        if (objectArray.IsDefaultOrEmpty)
        {
            var type = parameterOrPropertyTypeSymbols.SafeFirstOrDefault()
                .GloballyQualifiedOrFallback(objectArray.SafeFirstOrDefault());

            yield return new Argument(type, null);
            yield break;
        }

        for (var index = 0; index < objectArray.Length; index++)
        {
            var typedConstant = objectArray[index];
            var type = GetTypeFromParameters(parameterOrPropertyTypeSymbols, index);

            yield return new Argument(type.GloballyQualifiedOrFallback(typedConstant),
                TypedConstantParser.GetRawTypedConstantValue(typedConstant));
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

    private static bool CanConvert(GeneratorAttributeSyntaxContext context, ITypeSymbol? argumentType, ITypeSymbol? methodParameterType)
    {
        if (argumentType is not null
            && methodParameterType is not null
            && context.SemanticModel.Compilation.ClassifyConversion(argumentType, methodParameterType)
                is { IsImplicit: true }
                or { IsExplicit: true }
                or { IsNumeric: true })
        {
            return true;
        }

        return context.SemanticModel.Compilation.HasImplicitConversionOrGenericParameter(argumentType, methodParameterType);
    }
}
