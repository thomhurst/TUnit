using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.CodeGenerators;
using TUnit.Engine.SourceGenerator.Models;

namespace TUnit.Engine.SourceGenerator;

internal static class WriteableTestsRetriever
{
    public static IEnumerable<WriteableTest> GetWriteableTests(ClassMethod classMethod)
    {
        var attributes = classMethod.MethodSymbol.GetAttributes();
        
        if (!attributes.Any(x =>
                x.AttributeClass?.BaseType?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                == WellKnownFullyQualifiedClassNames.BaseTestAttribute))
        {
            yield break;
        }

        AttributeData[] testDataAttributes =
        [
            // Tests that don't have data
            ..GetAttributes(attributes, WellKnownFullyQualifiedClassNames.TestAttribute),

            // Combinative tests - These have to be evaluated specially to work out all the combinations
            ..GetAttributes(attributes, WellKnownFullyQualifiedClassNames.CombinativeTestAttribute),

            // Test data which will produce tests
            ..GetAttributes(attributes, WellKnownFullyQualifiedClassNames.ArgumentsAttribute),
            ..GetAttributes(attributes, WellKnownFullyQualifiedClassNames.CombinativeValuesAttribute),
            ..GetAttributes(attributes, WellKnownFullyQualifiedClassNames.ClassDataAttribute),
            ..GetAttributes(attributes, WellKnownFullyQualifiedClassNames.MethodDataAttribute),
        ];

        var classIndex = 0;
        foreach (var classArgument in ClassArgumentsGenerator.GetClassArguments(classMethod.NamedTypeSymbol))
        {
            classIndex++;

            var methodIndex = 0;
            foreach (var argumentAttribute in testDataAttributes)
            {
                for (var i = 0; i < TestInformationGenerator.GetRepeatCount(classMethod.MethodSymbol, classMethod.NamedTypeSymbol) + 1; i++)
                {
                    if (argumentAttribute.AttributeClass?.ToDisplayString(DisplayFormats
                            .FullyQualifiedNonGenericWithGlobalPrefix)
                        == WellKnownFullyQualifiedClassNames.CombinativeTestAttribute)
                    {
                        foreach (var combinativeTestData in ParseCombinativeTestsData(classMethod.MethodSymbol))
                        {
                            yield return new WriteableTest(classMethod.MethodSymbol,
                                classMethod.NamedTypeSymbol,
                                [classArgument], // TODO: Be able to accept a true array here
                                combinativeTestData.ToList(),
                                classIndex,
                                ++methodIndex
                            );
                        }

                        yield break;
                    }
                    
                    yield return new WriteableTest(classMethod.MethodSymbol,
                        classMethod.NamedTypeSymbol,
                        [classArgument], // TODO: Be able to accept a true array here
                        TestArgumentsGenerator.GetTestMethodArguments(classMethod.MethodSymbol, argumentAttribute).ToList(),
                        classIndex,
                        ++methodIndex
                    );
                }
            }
        }
    }

    private static IEnumerable<IEnumerable<Argument>> ParseCombinativeTestsData(IMethodSymbol methodSymbol)
    {
        return CombinativeValuesGenerator.GetTestsArguments(methodSymbol);
    }

    private static IEnumerable<AttributeData> GetAttributes(ImmutableArray<AttributeData> attributes, string fullyQualifiedNameWithGlobalPrefix)
    {
        return attributes.Where(x =>
            x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
            == fullyQualifiedNameWithGlobalPrefix);
    }
}