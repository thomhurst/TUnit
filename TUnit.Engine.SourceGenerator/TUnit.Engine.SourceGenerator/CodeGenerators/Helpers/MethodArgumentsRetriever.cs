using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.Enums;
using TUnit.Engine.SourceGenerator.Models;

namespace TUnit.Engine.SourceGenerator.CodeGenerators.Helpers;

internal static class MethodArgumentsRetriever
{
    public static IEnumerable<ArgumentsContainer> GetMethodArguments(IMethodSymbol methodSymbol, 
        INamedTypeSymbol namedTypeSymbol,
        TestType testType)
    {
        if (methodSymbol.Parameters.IsDefaultOrEmpty)
        {
            return [new ArgumentsContainer
            {
                Arguments = [],
                DataAttribute = null,
                DataAttributeIndex = null,
                IsEnumerableData = false
            }];
        }

        var methodAttributes = methodSymbol.GetAttributes();
        var classAttributes = namedTypeSymbol.GetAttributes();

        AttributeData[] testAndClassAttributes = [..methodAttributes, ..classAttributes];
        
        return testType switch
        {
            TestType.Basic => [BasicTestArgumentsRetriever.Parse(testAndClassAttributes)],
            TestType.DataDriven => DataDrivenArgumentsRetriever.Parse(methodAttributes, testAndClassAttributes, methodSymbol.Parameters),
            TestType.DataSourceDriven => DataSourceDrivenArgumentsRetriever.Parse(namedTypeSymbol, methodAttributes, testAndClassAttributes),
            TestType.Combinative => CombinativeValuesRetriever.Parse(methodSymbol, testAndClassAttributes),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}