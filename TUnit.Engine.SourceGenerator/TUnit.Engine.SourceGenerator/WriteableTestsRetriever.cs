using System.Collections.Generic;
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
        
        var testAttributes = attributes.Where(x =>
            x.AttributeClass?.BaseType?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
            == WellKnownFullyQualifiedClassNames.BaseTestAttribute).ToList();
        
        if (!testAttributes.Any())
        {
            yield break;
        }

        var testAttribute = testAttributes.First();
        
        foreach (var argumentAttribute in attributes.Where(x => x.AttributeClass?.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix)
                                                == WellKnownFullyQualifiedClassNames.ArgumentsAttribute))
        {
            yield return new WriteableTest(methodSymbol,
                ClassInvocationsGenerator.GenerateClassInvocations(),
                TestArgumentsGenerator.GetTestMethodArguments(methodSymbol, argumentAttribute)
            );
        }
        
    }
}