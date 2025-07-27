using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Extensions;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Helpers;

public static class TupleArgumentHelper
{
    public static List<string> GenerateArgumentAccess(ITypeSymbol parameterType, string argumentsArrayName, int baseIndex)
    {
        var argumentExpressions = new List<string>();
        
        // For method parameters, tuples are NOT supported - the data source
        // must return already unpacked values matching the method signature
        var castExpression = $"TUnit.Core.Helpers.CastHelper.Cast<{parameterType.GloballyQualified()}>({argumentsArrayName}[{baseIndex}])";
        argumentExpressions.Add(castExpression);
        
        return argumentExpressions;
    }

    /// <param name="parameterTypes">The types of all constructor parameters</param>
    /// <param name="argumentsArrayName">The name of the arguments array (e.g., "args")</param>
    /// <returns>A list of argument access expressions for the constructor</returns>
    public static List<string> GenerateConstructorArgumentAccess(IList<ITypeSymbol> parameterTypes, string argumentsArrayName)
    {
        var argumentExpressions = new List<string>();
        
        // Data sources already provide unwrapped arguments, so we just access by index
        for (int i = 0; i < parameterTypes.Count; i++)
        {
            var parameterType = parameterTypes[i];
            var castExpression = $"TUnit.Core.Helpers.CastHelper.Cast<{parameterType.GloballyQualified()}>({argumentsArrayName}[{i}])";
            argumentExpressions.Add(castExpression);
        }
        
        return argumentExpressions;
    }

    /// <summary>
    /// Generates method invocation arguments.
    /// </summary>
    /// <param name="parameters">The method parameters</param>
    /// <param name="argumentsArrayName">The name of the arguments array</param>
    /// <returns>Comma-separated argument expressions for method invocation</returns>
    public static string GenerateMethodInvocationArguments(IList<IParameterSymbol> parameters, string argumentsArrayName)
    {
        var allArguments = new List<string>();
        
        for (int i = 0; i < parameters.Count; i++)
        {
            var parameter = parameters[i];
            var castExpression = $"TUnit.Core.Helpers.CastHelper.Cast<{parameter.Type.GloballyQualified()}>({argumentsArrayName}[{i}])";
            allArguments.Add(castExpression);
        }
        
        return string.Join(", ", allArguments);
    }
}