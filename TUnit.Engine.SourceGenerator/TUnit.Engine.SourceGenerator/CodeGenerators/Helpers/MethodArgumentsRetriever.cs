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
        
        var allAttributes = methodSymbol.GetAttributesIncludingClass(namedTypeSymbol);
        
        return testType switch
        {
            TestType.Basic => [BasicTestArgumentsRetriever.Parse(allAttributes)],
            TestType.DataDriven => DataDrivenArgumentsRetriever.Parse(allAttributes),
            TestType.DataSourceDriven => DataSourceDrivenArgumentsRetriever.Parse(allAttributes),
            TestType.Combinative => CombinativeValuesRetriever.Parse(methodSymbol, allAttributes),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}