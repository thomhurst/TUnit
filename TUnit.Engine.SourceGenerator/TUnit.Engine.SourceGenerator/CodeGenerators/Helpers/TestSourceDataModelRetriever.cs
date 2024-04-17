using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.Enums;
using TUnit.Engine.SourceGenerator.Extensions;
using TUnit.Engine.SourceGenerator.Models;

namespace TUnit.Engine.SourceGenerator.CodeGenerators.Helpers;

internal static class TestSourceDataModelRetriever
{
    public static IEnumerable<TestSourceDataModel> ParseTestDatas(this IMethodSymbol methodSymbol, 
        INamedTypeSymbol namedTypeSymbol,
        TestType testType)
    {
        var testAttribute = methodSymbol.GetRequiredTestAttribute();
        var allAttributes = methodSymbol.GetAttributesIncludingClass(namedTypeSymbol);
        
        if (testType is TestType.Unknown)
        {
            testType = testAttribute.GetTestType();
        }
        
        var testArguments = TestArgumentsRetriever.GetTestMethodArguments()
        
        return null!;
    }
}