using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.Enums;
using TUnit.Engine.SourceGenerator.Extensions;
using TUnit.Engine.SourceGenerator.Models;

namespace TUnit.Engine.SourceGenerator.CodeGenerators.Helpers;

internal static class MethodArgumentsRetriever
{
    public static IEnumerable<IEnumerable<Argument>> GetMethodArguments(IMethodSymbol methodSymbol, 
        INamedTypeSymbol namedTypeSymbol,
        TestType testType)
    {
        if (methodSymbol.Parameters.IsDefaultOrEmpty)
        {
            return [];
        }

        var methodAttributes = methodSymbol.GetAttributes();
        var classAttributes = namedTypeSymbol.GetAttributes();

        AttributeData[] testAndClassAttributes = [..methodAttributes, ..classAttributes];
        
        return testType switch
        {
            TestType.Basic => [BasicTestArgumentsRetriever.Parse(testAndClassAttributes)],
            TestType.DataDriven => DataDrivenArgumentsRetriever.Parse(methodAttributes, testAndClassAttributes),
            TestType.DataSourceDriven => DataSourceDrivenArgumentsRetriever.Parse(methodAttributes, testAndClassAttributes),
            TestType.Combinative => CombinativeValuesRetriever.Parse(methodSymbol, testAndClassAttributes),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}