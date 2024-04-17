using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using TUnit.Engine.SourceGenerator.Enums;

namespace TUnit.Engine.SourceGenerator.CodeGenerators.Helpers;

public static class MethodParenthesisRetriever
{
    public static string WriteParenthesis(IMethodSymbol methodSymbol)
    {
        if (!methodSymbol.Parameters.IsDefaultOrEmpty)
        {
            return "()";
        }
        
        return $"({string.Join(", ", methodSymbol.Parameters.Select(GetArgumentForParameter))})";
    }
    
    public static string WriteParenthesis(KnownArguments knownArguments)
    {
        if (knownArguments == KnownArguments.None)
        {
            return "()";
        }
        
        return $"({string.Join(", ", GetArgumentVariables(knownArguments))})";
    }

    private static string GetArgumentForParameter(IParameterSymbol parameter)
    {
        var displayString = parameter.Type.ToDisplayString(DisplayFormats.FullyQualifiedNonGenericWithGlobalPrefix);
        
        if (displayString == WellKnownFullyQualifiedClassNames.TestContext)
        {
            return "testContext";
        }
        
        if (displayString == "global::System.Threading.CancellationToken")
        {
            return "global::TUnit.Engine.EngineCancellationToken.CancellationTokenSource.Token";
        }

        throw new NotImplementedException($"No known argument for {displayString}");
    }
    
    private static IEnumerable<string> GetArgumentVariables(KnownArguments knownArguments)
    {
        if (knownArguments.HasFlag(KnownArguments.TestContext))
        {
            yield return "testContext";
        }

        if (knownArguments.HasFlag(KnownArguments.CancellationToken))
        {
            yield return "global::TUnit.Engine.EngineCancellationToken.CancellationTokenSource.Token";
        }
    }
}