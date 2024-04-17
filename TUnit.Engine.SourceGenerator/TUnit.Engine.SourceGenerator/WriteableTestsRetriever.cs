using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.CodeGenerators;
using TUnit.Engine.SourceGenerator.CodeGenerators.Helpers;
using TUnit.Engine.SourceGenerator.Extensions;
using TUnit.Engine.SourceGenerator.Models;

namespace TUnit.Engine.SourceGenerator;

internal static class WriteableTestsRetriever
{
    public static IEnumerable<WriteableTest> GetWriteableTests(ClassMethod classMethod)
    {
        var methodOnlyAttributes = classMethod.MethodSymbol.GetAttributes();
        
        if (!methodOnlyAttributes.Any(x => x.AttributeClass?.IsTestClass() == true))
        {
            yield break;
        }
        
        AttributeData[] methodAndClassAttributes =
        [
            ..classMethod.MethodSymbol.GetAttributes(),
            ..classMethod.NamedTypeSymbol.GetAttributes(),
            ..classMethod.MethodSymbol.ContainingType.GetAttributes(),
        ];

        IEnumerable<AttributeData> testDataAttributes =
        [
            // Tests that don't have data
            ..GetAttributes(methodOnlyAttributes, WellKnownFullyQualifiedClassNames.TestAttribute),

            // Combinative tests - These have to be evaluated specially to work out all the combinations
            ..GetAttributes(methodOnlyAttributes, WellKnownFullyQualifiedClassNames.CombinativeTestAttribute),

            // Test data which will produce tests
            ..GetAttributes(methodOnlyAttributes, WellKnownFullyQualifiedClassNames.ArgumentsAttribute),
            ..GetAttributes(methodOnlyAttributes, WellKnownFullyQualifiedClassNames.CombinativeValuesAttribute),
            ..GetAttributes(methodOnlyAttributes, WellKnownFullyQualifiedClassNames.ClassDataAttribute),
            ..GetAttributes(methodOnlyAttributes, WellKnownFullyQualifiedClassNames.MethodDataAttribute),
        ];

        var classIndex = 0;
        foreach (var classArgument in ClassArgumentsRetriever.GetClassArguments(classMethod.NamedTypeSymbol))
        {
            classIndex++;

            var methodIndex = 0;
            foreach (var argumentAttribute in testDataAttributes)
            {
                var runCount = TestInformationRetriever.GetRepeatCount(methodAndClassAttributes) + 1;
                for (var i = 0; i < runCount; i++)
                {
                    if (argumentAttribute.AttributeClass?.ToDisplayString(DisplayFormats
                            .FullyQualifiedNonGenericWithGlobalPrefix)
                        == WellKnownFullyQualifiedClassNames.CombinativeTestAttribute)
                    {
                        foreach (var combinativeTestData in ParseCombinativeTestsData(classMethod.MethodSymbol, methodAndClassAttributes))
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
                        TestArgumentsRetriever.GetTestMethodArguments(classMethod.MethodSymbol, argumentAttribute, methodAndClassAttributes).ToList(),
                        classIndex,
                        ++methodIndex
                    );
                }
            }
        }
    }

    private static IEnumerable<IEnumerable<Argument>> ParseCombinativeTestsData(IMethodSymbol methodSymbol, AttributeData[] methodAndClassAttributes)
    {
        return CombinativeValuesRetriever.GetTestsArguments(methodSymbol, methodAndClassAttributes);
    }

    private static IEnumerable<AttributeData> GetAttributes(ImmutableArray<AttributeData> attributes, string fullyQualifiedNameWithGlobalPrefix)
    {
        return attributes.Where(x =>
            x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
            == fullyQualifiedNameWithGlobalPrefix);
    }
}