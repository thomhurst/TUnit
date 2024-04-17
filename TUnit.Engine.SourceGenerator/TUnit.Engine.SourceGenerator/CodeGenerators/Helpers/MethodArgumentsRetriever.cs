using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.Enums;
using TUnit.Engine.SourceGenerator.Extensions;
using TUnit.Engine.SourceGenerator.Models;

namespace TUnit.Engine.SourceGenerator.CodeGenerators.Helpers;

internal static class MethodArgumentsRetriever
{
    public static IEnumerable<IEnumerable<Argument>> GetMethodArguments(IMethodSymbol methodSymbol, INamedTypeSymbol namedTypeSymbol)
    {
        if (methodSymbol.Parameters.IsDefaultOrEmpty)
        {
            return [];
        }
        
        var testAttribute = methodSymbol.GetTestAttribute();
        var allAttributes = methodSymbol.GetAttributesIncludingClass(namedTypeSymbol);

        var testType = testAttribute.GetTestType();
        
        switch (testType)
        {
            case TestType.Basic:
                return [BasicTestArgumentsRetriever.Parse(allAttributes)];
            case TestType.DataDriven:
                return DataDrivenArgumentsRetriever.Parse(allAttributes);
            case TestType.DataSourceDriven:
                return DataSourceDrivenArgumentsRetriever.Parse(allAttributes);
            case TestType.Combinative:
                return CombinativeValuesRetriever.Parse(methodSymbol, allAttributes);
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}