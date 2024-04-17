using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.Models;

namespace TUnit.Engine.SourceGenerator.CodeGenerators.Helpers;

internal static class CombinativeValuesRetriever
{
    // We return a List of a List. Inner List is for each test.
    public static IEnumerable<IEnumerable<Argument>> GetTestsArguments(IMethodSymbol methodSymbol, AttributeData[] methodAndClassAttributes)
    {
        var combinativeValuesAttributes = methodSymbol.Parameters
            .Select(x => x.GetAttributes().FirstOrDefault(x => x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                                                               == WellKnownFullyQualifiedClassNames.CombinativeValuesAttribute))
            .OfType<AttributeData>()
            .ToList();
        
        
        
        var mappedToConstructorArrays = combinativeValuesAttributes
            .Select(x => x.ConstructorArguments.First().Values);

        return GetCombinativeArgumentsList(mappedToConstructorArrays)
            .Select(x =>
                MapToArgumentEnumerable(x, methodAndClassAttributes)
            );
    }

    private static IEnumerable<Argument> MapToArgumentEnumerable(IEnumerable<TypedConstant> x, AttributeData[] methodAndClassAttributes)
    {
        var enumerable = x.Select(y =>
            new Argument(y.Type!.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix),
                TypedConstantParser.GetTypedConstantValue(y)));

        var timeoutCancellationToken = TimeoutCancellationTokenRetriever.GetCancellationTokenArgument(methodAndClassAttributes);

        if (timeoutCancellationToken != null)
        {
            return [..enumerable, timeoutCancellationToken];
        }
        
        return enumerable;
    }

    private static readonly IEnumerable<IEnumerable<TypedConstant>> Seed = new[] { Enumerable.Empty<TypedConstant>() };
    
    private static IEnumerable<IEnumerable<TypedConstant>> GetCombinativeArgumentsList(IEnumerable<ImmutableArray<TypedConstant>> elements)
    {
        return elements.Aggregate(Seed, (accumulator, enumerable)
            => accumulator.SelectMany(x => enumerable.Select(x.Append)));
    }
}