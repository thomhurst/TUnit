using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Helpers;

public static class TupleArgumentHelper
{
    /// <summary>
    /// Generates an AOT-compatible cast expression for converting from object? to the target type.
    /// Since the source generator knows types at compile time, we can emit direct casts which are AOT-safe.
    /// </summary>
    private static string GenerateCastExpression(ITypeSymbol targetType, string sourceExpression)
    {
        // For all types, generate a direct cast since the source generator knows the exact type at compile time.
        // This is AOT-safe because:
        // 1. The C# compiler handles boxing/unboxing automatically
        // 2. No reflection is needed - the cast is resolved at compile time
        // 3. If a cast fails at runtime, InvalidCastException is thrown (expected behavior)
        // 4. For custom conversions, users can register converters in AotConverterRegistry
        return $"({targetType.GloballyQualified()}){sourceExpression}";
    }

    public static List<string> GenerateArgumentAccess(ITypeSymbol parameterType, string argumentsArrayName, int baseIndex)
    {
        var argumentExpressions = new List<string>();

        // For method parameters, tuples are NOT supported - the data source
        // must return already unpacked values matching the method signature
        var castExpression = GenerateCastExpression(parameterType, $"{argumentsArrayName}[{baseIndex}]");
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
            var castExpression = GenerateCastExpression(parameterType, $"{argumentsArrayName}[{i}]");
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
            var castExpression = GenerateCastExpression(parameter.Type, $"{argumentsArrayName}[{i}]");
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
        string? argCountExpression = null;
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
                    var castExpression = GenerateCastExpression(param.Type, $"{argumentsArrayName}[{i}]");
                    argumentExpressions.Add(castExpression);
                }
            }
            else
            {
                for (var i = 0; i < parameters.Count && i < argCount; i++)
                {
                    var param = parameters[i];
                    var castExpression = GenerateCastExpression(param.Type, $"{argumentsArrayName}[{i}]");
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
                    var castExpression = GenerateCastExpression(param.Type, $"{argumentsArrayName}[{i}]");
                    argumentExpressions.Add(castExpression);
                }
            }
            else
            {
                for (var i = 0; i < regularParamCount && i < argCount; i++)
                {
                    var param = parameters[i];
                    var castExpression = GenerateCastExpression(param.Type, $"{argumentsArrayName}[{i}]");
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
                    var arrayInit = $"({argumentsArrayName}.Length > {regularParamCount} ? global::System.Linq.Enumerable.Range({regularParamCount}, {argCountExpression} - {regularParamCount}).Select(i => ({elementType.GloballyQualified()}){argumentsArrayName}[i]).ToArray() : new {elementType.GloballyQualified()}[0])";
                    argumentExpressions.Add(arrayInit);
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
                        // Single argument for params - check if it's null or already the correct array type
                        // In C#, params T[] can receive:
                        // - null (passed as null, not as array with null element)
                        // - T[] (passed directly, not wrapped in another array)
                        // - T (wrapped in array with single element)
                        var singleArg = $"{argumentsArrayName}[{regularParamCount}]";
                        var checkAndCast = $"({singleArg} is null ? null : {singleArg} is {paramsParam.Type.GloballyQualified()} arr ? arr : new {elementType.GloballyQualified()}[] {{ ({elementType.GloballyQualified()}){singleArg} }})";
                        argumentExpressions.Add(checkAndCast);
                    }
                    else
                    {
                        // Multiple arguments for params - create array
                        var arrayElements = new List<string>();
                        for (var i = regularParamCount; i < argCount; i++)
                        {
                            arrayElements.Add($"({elementType.GloballyQualified()}){argumentsArrayName}[{i}]");
                        }
                        argumentExpressions.Add($"new {elementType.GloballyQualified()}[] {{ {string.Join(", ", arrayElements)} }}");
                    }
                }
            }
            else
            {
                // Fallback if we can't determine element type
                var castExpression = GenerateCastExpression(paramsParam.Type, $"{argumentsArrayName}[{regularParamCount}]");
                argumentExpressions.Add(castExpression);
            }
        }
        
        return argumentExpressions;
    }
}