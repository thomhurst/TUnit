using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using TUnit.Core.SourceGenerator.Enums;
using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator.Models.Arguments;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Helpers;

public static class MatrixRetriever
{
    // We return a List of a List. Inner List is for each test.
    public static IEnumerable<ArgumentsContainer> Parse(GeneratorAttributeSyntaxContext context, ImmutableArray<IParameterSymbol> parameters, ArgumentsType argumentsType)
    {
        if (parameters.IsDefaultOrEmpty || !parameters.HasMatrixAttribute())
        {
            return [];
        }

        var matrixAttributes = parameters
            .Select(p => p.GetAttributes().SafeFirstOrDefault(a => a.GetFullyQualifiedAttributeTypeName()
                                                               == WellKnownFullyQualifiedClassNames.MatrixAttribute.WithGlobalPrefix))
            .OfType<AttributeData>()
            .ToList();

        var mappedToArgumentArrays = matrixAttributes
            .Select(x =>
            {
                var attributeSyntax = (AttributeSyntax)x.ApplicationSyntaxReference!.GetSyntax();
                var arguments = attributeSyntax.ArgumentList!.Arguments;

                var objectArray = x.ConstructorArguments.SafeFirstOrDefault().Values;

                return objectArray.Zip(arguments, (o, a) => (o, a));
            });

        var attr = matrixAttributes.SafeFirstOrDefault();

        if (attr is null)
        {
            return [];
        }

        var index = 0;
        return GetMatrixArgumentsList(mappedToArgumentArrays)
            .Select(x => MapToArgumentEnumerable(context, x, parameters))
            .Select(x => new ArgumentsAttributeContainer(argumentsType, [.. x])
            {
                Attribute = null,
                AttributeIndex = index++,
                DisposeAfterTest = attr.NamedArguments.FirstOrDefault(x => x.Key == "DisposeAfterTest").Value.Value as bool? ?? true,
            });
    }
    private static IEnumerable<Argument> MapToArgumentEnumerable(GeneratorAttributeSyntaxContext context, IEnumerable<(TypedConstant ArgumentConstant, AttributeArgumentSyntax ArgumentSyntax)> elements, ImmutableArray<IParameterSymbol> parameterSymbols)
    {
        return elements.Select((element, index) =>
        {
            var type = parameterSymbols.ElementAt(index).Type;

            return new Argument(type?.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix) ??
                                TypedConstantParser.GetFullyQualifiedTypeNameFromTypedConstantValue(element.ArgumentConstant),
                TypedConstantParser.GetTypedConstantValue(context.SemanticModel, element.ArgumentSyntax.Expression, type));
        });
    }

    private static readonly IEnumerable<IEnumerable<(TypedConstant, AttributeArgumentSyntax)>> Seed = [[]];

    private static IEnumerable<IEnumerable<(TypedConstant, AttributeArgumentSyntax)>> GetMatrixArgumentsList(IEnumerable<IEnumerable<(TypedConstant, AttributeArgumentSyntax)>> elements)
    {
        return elements.Aggregate(Seed, (accumulator, enumerable)
            => accumulator.SelectMany(x => enumerable.Select(x.Append)));
    }
}