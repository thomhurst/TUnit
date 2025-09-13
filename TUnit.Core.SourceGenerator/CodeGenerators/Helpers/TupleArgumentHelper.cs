using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator.Models;

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
        for (var i = 0; i < parameterTypes.Count; i++)
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
        
        for (var i = 0; i < parameters.Count; i++)
        {
            var parameter = parameters[i];
            var castExpression = $"TUnit.Core.Helpers.CastHelper.Cast<{parameter.Type.GloballyQualified()}>({argumentsArrayName}[{i}])";
            allArguments.Add(castExpression);
        }
        
        return string.Join(", ", allArguments);
    }
    
    /// <summary>
    /// Generates argument access for a method with possible params array, given a specific argument count.
    /// </summary>
    /// <param name="parameters">The method parameters</param>
    /// <param name="argumentsArrayName">The name of the arguments array</param>
    /// <param name="argumentCount">The actual number of arguments provided</param>
    /// <returns>List of argument expressions for method invocation</returns>
    public static List<string> GenerateArgumentAccessWithParams(IList<IParameterSymbol> parameters, string argumentsArrayName, object argumentCount)
    {
        var argumentExpressions = new List<string>();
        
        // Parse argument count - can be an int or a string expression
        int argCount;
        string argCountExpression = null;
        if (argumentCount is int count)
        {
            argCount = count;
        }
        else if (argumentCount is string expr)
        {
            argCountExpression = expr;
            argCount = int.MaxValue; // Flag for dynamic count
        }
        else
        {
            throw new ArgumentException("argumentCount must be int or string");
        }
        
        // Check if last parameter is params array
        var hasParams = parameters.Count > 0 && parameters[parameters.Count - 1].IsParams;
        
        if (!hasParams)
        {
            // No params array - just cast each argument
            if (argCountExpression != null)
            {
                // Dynamic count - use Math.Min
                for (var i = 0; i < parameters.Count; i++)
                {
                    var param = parameters[i];
                    var castExpression = $"TUnit.Core.Helpers.CastHelper.Cast<{param.Type.GloballyQualified()}>({argumentsArrayName}[{i}])";
                    argumentExpressions.Add(castExpression);
                }
            }
            else
            {
                for (var i = 0; i < parameters.Count && i < argCount; i++)
                {
                    var param = parameters[i];
                    var castExpression = $"TUnit.Core.Helpers.CastHelper.Cast<{param.Type.GloballyQualified()}>({argumentsArrayName}[{i}])";
                    argumentExpressions.Add(castExpression);
                }
            }
        }
        else
        {
            // Has params array
            var regularParamCount = parameters.Count - 1;
            
            // Handle regular parameters
            if (argCountExpression != null)
            {
                // Dynamic count
                for (var i = 0; i < regularParamCount; i++)
                {
                    var param = parameters[i];
                    var castExpression = $"TUnit.Core.Helpers.CastHelper.Cast<{param.Type.GloballyQualified()}>({argumentsArrayName}[{i}])";
                    argumentExpressions.Add(castExpression);
                }
            }
            else
            {
                for (var i = 0; i < regularParamCount && i < argCount; i++)
                {
                    var param = parameters[i];
                    var castExpression = $"TUnit.Core.Helpers.CastHelper.Cast<{param.Type.GloballyQualified()}>({argumentsArrayName}[{i}])";
                    argumentExpressions.Add(castExpression);
                }
            }
            
            // Handle params array parameter
            var paramsParam = parameters[parameters.Count - 1];
            var elementType = (paramsParam.Type as IArrayTypeSymbol)?.ElementType;
            
            if (elementType != null)
            {
                if (argCountExpression != null)
                {
                    // Dynamic count - create array from remaining arguments
                    var arrayInit = $@"
                        ({argumentsArrayName}.Length > {regularParamCount} 
                            ? global::System.Linq.Enumerable.Range({regularParamCount}, {argCountExpression} - {regularParamCount})
                                .Select(i => TUnit.Core.Helpers.CastHelper.Cast<{elementType.GloballyQualified()}>({argumentsArrayName}[i]))
                                .ToArray()
                            : new {elementType.GloballyQualified()}[0])";
                    argumentExpressions.Add(arrayInit.Replace("\r\n", " ").Replace("\n", " ").Trim());
                }
                else
                {
                    var remainingArgCount = Math.Max(0, argCount - regularParamCount);
                    
                    if (remainingArgCount == 0)
                    {
                        // No arguments for params array - pass empty array
                        argumentExpressions.Add($"new {elementType.GloballyQualified()}[0]");
                    }
                    else if (remainingArgCount == 1)
                    {
                        // Single argument for params - can pass directly or as array
                        var singleArgExpression = $"TUnit.Core.Helpers.CastHelper.Cast<{paramsParam.Type.GloballyQualified()}>({argumentsArrayName}[{regularParamCount}])";
                        argumentExpressions.Add(singleArgExpression);
                    }
                    else
                    {
                        // Multiple arguments for params - create array
                        var arrayElements = new List<string>();
                        for (var i = regularParamCount; i < argCount; i++)
                        {
                            arrayElements.Add($"TUnit.Core.Helpers.CastHelper.Cast<{elementType.GloballyQualified()}>({argumentsArrayName}[{i}])");
                        }
                        argumentExpressions.Add($"new {elementType.GloballyQualified()}[] {{ {string.Join(", ", arrayElements)} }}");
                    }
                }
            }
            else
            {
                // Fallback if we can't determine element type
                var castExpression = $"TUnit.Core.Helpers.CastHelper.Cast<{paramsParam.Type.GloballyQualified()}>({argumentsArrayName}[{regularParamCount}])";
                argumentExpressions.Add(castExpression);
            }
        }
        
        return argumentExpressions;
    }
}