using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace TUnit.Engine.SourceGenerator.CodeGenerators;

internal static class CombinativeValuesGenerator
{
    // We return a List of a List. Inner List is for each test.
    public static IEnumerable<IEnumerable<string>> GetTestsArguments(
        IEnumerable<AttributeData> combinativeValuesAttributes)
    {
        var mappedToValues = combinativeValuesAttributes.Select(x =>
            x.ConstructorArguments.Select(TypedConstantParser.GetTypedConstantValue).ToList());

        return GetCombinativeArgumentsList(mappedToValues);
    }
    
    private static readonly IEnumerable<IEnumerable<string>> Seed = new[] { Enumerable.Empty<string>() };
    
    public static IEnumerable<IEnumerable<string>> GetCombinativeArgumentsList(IEnumerable<IReadOnlyList<string>> elements)
    {
        return elements.Aggregate(Seed, (accumulator, enumerable)
            => accumulator.SelectMany(x => enumerable.Select(x.Append)));
    }
}