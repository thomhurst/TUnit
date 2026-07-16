using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Extensions;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Helpers;

public static class TupleArgumentHelper
{
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
            var castExpression = $"global::TUnit.Core.Helpers.CastHelper.Cast<{parameter.Type.GloballyQualified()}>({argumentsArrayName}[{i}])";
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

        // A trailing array parameter (params OR a plain `T[]`) collects remaining loose values
        // into the array, letting `[Arguments(["a", "b"])]` map onto a single `string[]`
        // parameter (issue #6120).
        var hasParams = parameters.Count > 0 && parameters[parameters.Count - 1].CollectsTrailingArguments();

        if (!hasParams)
        {
            // No params array - just cast each argument
            if (argCountExpression != null)
            {
                // Dynamic count - use Math.Min
                for (var i = 0; i < parameters.Count; i++)
                {
                    var param = parameters[i];
                    var castExpression = $"global::TUnit.Core.Helpers.CastHelper.Cast<{param.Type.GloballyQualified()}>({argumentsArrayName}[{i}])";
                    argumentExpressions.Add(castExpression);
                }
            }
            else
            {
                for (var i = 0; i < parameters.Count && i < argCount; i++)
                {
                    var param = parameters[i];
                    var castExpression = $"global::TUnit.Core.Helpers.CastHelper.Cast<{param.Type.GloballyQualified()}>({argumentsArrayName}[{i}])";
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
                    var castExpression = $"global::TUnit.Core.Helpers.CastHelper.Cast<{param.Type.GloballyQualified()}>({argumentsArrayName}[{i}])";
                    argumentExpressions.Add(castExpression);
                }
            }
            else
            {
                for (var i = 0; i < regularParamCount && i < argCount; i++)
                {
                    var param = parameters[i];
                    var castExpression = $"global::TUnit.Core.Helpers.CastHelper.Cast<{param.Type.GloballyQualified()}>({argumentsArrayName}[{i}])";
                    argumentExpressions.Add(castExpression);
                }
            }

            // Handle params array parameter
            var paramsParam = parameters[parameters.Count - 1];
            var elementType = GetTrailingElementType(paramsParam.Type);

            if (elementType != null)
            {
                if (argCountExpression != null)
                {
                    // Dynamic count - create array from remaining arguments. With no leading
                    // regular parameters the range spans the whole args array, so drop the
                    // redundant "- 0" offset for cleaner generated code.
                    var rangeCount = regularParamCount == 0
                        ? argCountExpression
                        : $"{argCountExpression} - {regularParamCount}";
                    // Use fully-qualified static Enumerable.Select/ToArray calls rather than the
                    // extension-method chain — generated files don't import System.Linq unless the
                    // project has ImplicitUsings enabled, so the extension form fails to compile (CS1061).
                    var arrayInit = $"({argumentsArrayName}.Length > {regularParamCount} ? global::System.Linq.Enumerable.ToArray(global::System.Linq.Enumerable.Select(global::System.Linq.Enumerable.Range({regularParamCount}, {rangeCount}), i => global::TUnit.Core.Helpers.CastHelper.Cast<{elementType.GloballyQualified()}>({argumentsArrayName}[i]))) : global::System.Array.Empty<{elementType.GloballyQualified()}>())";
                    argumentExpressions.Add(arrayInit);
                }
                else
                {
                    var remainingArgCount = Math.Max(0, argCount - regularParamCount);

                    if (remainingArgCount == 0)
                    {
                        // No arguments for params array - pass empty array. Use Array.Empty<T>()
                        // rather than `new T[0]`: when T is itself an array (jagged param like
                        // byte[][], element type byte[]) the rank-specifier form `new byte[][0]`
                        // is invalid C# (CS1586/CS0178). See issue #6150.
                        argumentExpressions.Add($"global::System.Array.Empty<{elementType.GloballyQualified()}>()");
                    }
                    else if (remainingArgCount == 1)
                    {
                        // Single argument for params - check if it's null or already the correct array type
                        // In C#, params T[] can receive:
                        // - null (passed as null, not as array with null element)
                        // - T[] (passed directly, not wrapped in another array)
                        // - T (wrapped in array with single element)
                        var singleArg = $"{argumentsArrayName}[{regularParamCount}]";
                        var checkAndCast = $"({singleArg} is null ? null : {singleArg} is {paramsParam.Type.GloballyQualified()} arr ? arr : new {elementType.GloballyQualified()}[] {{ global::TUnit.Core.Helpers.CastHelper.Cast<{elementType.GloballyQualified()}>({singleArg}) }})";
                        argumentExpressions.Add(checkAndCast);
                    }
                    else
                    {
                        // Multiple arguments for params - create array
                        var arrayElements = new List<string>();
                        for (var i = regularParamCount; i < argCount; i++)
                        {
                            arrayElements.Add($"global::TUnit.Core.Helpers.CastHelper.Cast<{elementType.GloballyQualified()}>({argumentsArrayName}[{i}])");
                        }
                        argumentExpressions.Add($"new {elementType.GloballyQualified()}[] {{ {string.Join(", ", arrayElements)} }}");
                    }
                }
            }
            else
            {
                // Fallback if we can't determine element type
                var castExpression = $"global::TUnit.Core.Helpers.CastHelper.Cast<{paramsParam.Type.GloballyQualified()}>({argumentsArrayName}[{regularParamCount}])";
                argumentExpressions.Add(castExpression);
            }
        }

        return argumentExpressions;
    }

    /// <summary>
    /// Gets the element type for a trailing collecting parameter. Handles plain arrays as well as
    /// C# 13 <c>params</c> collections whose target is an array-assignable generic interface
    /// (<c>IEnumerable&lt;T&gt;</c>, <c>IReadOnlyList&lt;T&gt;</c>, etc.) — for those a generated
    /// <c>T[]</c> satisfies the parameter. Returns null when no safe array construction applies.
    /// </summary>
    private static ITypeSymbol? GetTrailingElementType(ITypeSymbol parameterType)
    {
        if (parameterType is IArrayTypeSymbol array)
        {
            return array.ElementType;
        }

        if (parameterType is INamedTypeSymbol { IsGenericType: true } named &&
            IsArrayAssignableCollectionInterface(named.OriginalDefinition.SpecialType))
        {
            return named.TypeArguments[0];
        }

        return null;
    }

    private static bool IsArrayAssignableCollectionInterface(SpecialType specialType)
        => specialType is SpecialType.System_Collections_Generic_IEnumerable_T
            or SpecialType.System_Collections_Generic_ICollection_T
            or SpecialType.System_Collections_Generic_IList_T
            or SpecialType.System_Collections_Generic_IReadOnlyCollection_T
            or SpecialType.System_Collections_Generic_IReadOnlyList_T;
}
