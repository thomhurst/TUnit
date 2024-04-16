using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace TUnit.Engine.SourceGenerator.CodeGenerators;

public static class MethodParenthesisGenerator
{
    public static string WriteParenthesis(IMethodSymbol methodSymbol)
    {
        if (!methodSymbol.Parameters.IsDefaultOrEmpty)
        {
            return "()";
        }
        
        return $"({string.Join(", ", methodSymbol.Parameters.Select(GetArgumentForParameter))})";
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
}