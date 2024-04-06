using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.CodeGenerators;
using TUnit.Engine.SourceGenerator.Models;

namespace TUnit.Engine.SourceGenerator;

internal static class WriteableTestsRetriever
{
    public static IEnumerable<WriteableTest> GetWriteableTests(IMethodSymbol methodSymbol)
    {
        var attributes = methodSymbol.GetAttributes();


        if (!attributes.Any(x => x.AttributeClass?.BaseType?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                                 == WellKnownFullyQualifiedClassNames.BaseTestAttribute))
        {
            yield break;
        }

        AttributeData[] testDataAttributes =
        [
            ..GetAttributes(attributes, WellKnownFullyQualifiedClassNames.ArgumentsAttribute),
            ..GetAttributes(attributes, WellKnownFullyQualifiedClassNames.CombinativeValuesAttribute),
            ..GetAttributes(attributes, WellKnownFullyQualifiedClassNames.ClassDataAttribute),
            ..GetAttributes(attributes, WellKnownFullyQualifiedClassNames.MethodDataAttribute),
        ];

        var classIndex = 0;
        foreach (var classArgument in ClassArgumentsGenerator.GetClassArguments(methodSymbol.ContainingType))
        {
            classIndex++;

            var methodIndex = 0;
            foreach (var argumentAttribute in testDataAttributes)
            {
                yield return new WriteableTest(methodSymbol,
                    [classArgument], // TODO: Be able to accept a true array here
                    TestArgumentsGenerator.GetTestMethodArguments(methodSymbol, argumentAttribute).ToList(),
                    classIndex,
                    ++methodIndex
                );
            }
        }
    }

    private static IEnumerable<AttributeData> GetAttributes(ImmutableArray<AttributeData> attributes, string fullyQualifiedNameWithGlobalPrefix)
    {
        return attributes.Where(x =>
            x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
            == fullyQualifiedNameWithGlobalPrefix);
    }
}