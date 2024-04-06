using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.Models;

namespace TUnit.Engine.SourceGenerator.CodeGenerators;

internal static class CombinativeValuesGenerator
{
    // We return a List of a List. Inner List is for each test.
    public static IEnumerable<IEnumerable<Argument>> GetTestsArguments(
        IEnumerable<AttributeData> combinativeValuesAttributes)
    {
        var mappedToValues = combinativeValuesAttributes.Select(x => x.ConstructorArguments.First().Values);

        return GetCombinativeArgumentsList(mappedToValues);
    }
    
    private static readonly IEnumerable<IEnumerable<Argument>> Seed = new[] { Enumerable.Empty<Argument>() };
    
    private static IEnumerable<IEnumerable<Argument>> GetCombinativeArgumentsList(IEnumerable<ImmutableArray<TypedConstant>> elements)
    {
        return elements.Aggregate(Seed, (accumulator, enumerable)
            => accumulator.SelectMany(x => enumerable.Select(y => x.Append(new Argument(y.Type!.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix), TypedConstantParser.GetTypedConstantValue(y))))));
    }
}