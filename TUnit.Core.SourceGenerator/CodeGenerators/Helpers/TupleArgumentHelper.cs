using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Core.SourceGenerator.Extensions;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Helpers;

public static class TupleArgumentHelper
{
    public static string GenerateMethodInvocationArguments(
        IList<IParameterSymbol> parameters,
        string argumentsArrayName,
        ITypeSymbol?[]? sourceTypes = null,
        CSharpCompilation? compilation = null)
    {
        var allArguments = new List<string>();

        for (var i = 0; i < parameters.Count; i++)
        {
            var parameter = parameters[i];
            var sourceType = CastExpressionHelper.GetSourceTypeAt(sourceTypes, i);
            var castExpression = CastExpressionHelper.GenerateCast(sourceType, parameter.Type, $"{argumentsArrayName}[{i}]", compilation);
            allArguments.Add(castExpression);
        }

        return string.Join(", ", allArguments);
    }

    public static List<string> GenerateArgumentAccessWithParams(
        IList<IParameterSymbol> parameters,
        string argumentsArrayName,
        object argumentCount,
        ITypeSymbol?[]? sourceTypes = null,
        CSharpCompilation? compilation = null)
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
            var upperBound = Math.Min(parameters.Count, argCount);
            for (var i = 0; i < upperBound; i++)
            {
                var param = parameters[i];
                var sourceType = CastExpressionHelper.GetSourceTypeAt(sourceTypes, i);
                var castExpression = CastExpressionHelper.GenerateCast(sourceType, param.Type, $"{argumentsArrayName}[{i}]", compilation);
                argumentExpressions.Add(castExpression);
            }
        }
        else
        {
            // Has params array
            var regularParamCount = parameters.Count - 1;

            // Handle regular parameters
            var upperBound = Math.Min(regularParamCount, argCount);
            for (var i = 0; i < upperBound; i++)
            {
                var param = parameters[i];
                var sourceType = CastExpressionHelper.GetSourceTypeAt(sourceTypes, i);
                var castExpression = CastExpressionHelper.GenerateCast(sourceType, param.Type, $"{argumentsArrayName}[{i}]", compilation);
                argumentExpressions.Add(castExpression);
            }

            // Handle params array parameter
            var paramsParam = parameters[parameters.Count - 1];
            var elementType = GetParamsElementType(paramsParam.Type);

            if (elementType != null)
            {
                // For params elements, use the element's source type if available
                // (params elements are beyond the regular parameter positions, so source types may not cover them)
                var elementTargetGQ = elementType.GloballyQualified();

                if (argCountExpression != null)
                {
                    // Dynamic count - create array from remaining arguments
                    // For dynamic count, we can't determine source types per element, so use CastHelper
                    var arrayInit = $"({argumentsArrayName}.Length > {regularParamCount} ? global::System.Linq.Enumerable.Range({regularParamCount}, {argCountExpression} - {regularParamCount}).Select(i => global::TUnit.Core.Helpers.CastHelper.Cast<{elementTargetGQ}>({argumentsArrayName}[i])).ToArray() : new {elementTargetGQ}[0])";
                    argumentExpressions.Add(arrayInit);
                }
                else
                {
                    var remainingArgCount = Math.Max(0, argCount - regularParamCount);

                    if (remainingArgCount == 0)
                    {
                        // No arguments for params array - pass empty array
                        argumentExpressions.Add($"new {elementTargetGQ}[0]");
                    }
                    else if (remainingArgCount == 1)
                    {
                        // Single argument for params - check if it's null or already the correct array type
                        var singleArg = $"{argumentsArrayName}[{regularParamCount}]";
                        var paramsTypeGQ = paramsParam.Type.GloballyQualified();
                        var elementCast = GenerateElementCast(elementType, regularParamCount, singleArg, sourceTypes, compilation);
                        var checkAndCast = $"({singleArg} is null ? null : {singleArg} is {paramsTypeGQ} arr ? arr : new {elementTargetGQ}[] {{ {elementCast} }})";
                        argumentExpressions.Add(checkAndCast);
                    }
                    else
                    {
                        // Multiple arguments for params - create array
                        var arrayElements = new List<string>();
                        for (var i = regularParamCount; i < argCount; i++)
                        {
                            var elementCast = GenerateElementCast(elementType, i, $"{argumentsArrayName}[{i}]", sourceTypes, compilation);
                            arrayElements.Add(elementCast);
                        }
                        argumentExpressions.Add($"new {elementTargetGQ}[] {{ {string.Join(", ", arrayElements)} }}");
                    }
                }
            }
            else
            {
                // Fallback if we can't determine element type
                var sourceType = sourceTypes != null && regularParamCount < sourceTypes.Length ? sourceTypes[regularParamCount] : null;
                var castExpression = CastExpressionHelper.GenerateCast(sourceType, paramsParam.Type, $"{argumentsArrayName}[{regularParamCount}]", compilation);
                argumentExpressions.Add(castExpression);
            }
        }

        return argumentExpressions;
    }

    /// <summary>
    /// Extracts the element type from a params parameter type.
    /// Handles T[] (IArrayTypeSymbol) and generic collection types like IEnumerable&lt;T&gt;, List&lt;T&gt;, etc.
    /// </summary>
    private static ITypeSymbol? GetParamsElementType(ITypeSymbol paramsType)
    {
        if (paramsType is IArrayTypeSymbol arrayType)
        {
            return arrayType.ElementType;
        }

        // C# 13 params collections: IEnumerable<T>, ReadOnlySpan<T>, List<T>, etc.
        if (paramsType is INamedTypeSymbol { IsGenericType: true, TypeArguments.Length: 1 } namedType)
        {
            return namedType.TypeArguments[0];
        }

        return null;
    }

    private static string GenerateElementCast(
        ITypeSymbol elementType,
        int argIndex,
        string argExpression,
        ITypeSymbol?[]? sourceTypes,
        CSharpCompilation? compilation)
    {
        // Default to elementType when source type is unknown — for params overflow positions
        // (beyond [Arguments] row length), the element type is statically known and a direct
        // cast is sufficient. These positions are only reachable when all data sources are
        // [Arguments], which SourceTypeAnalyzer has already verified.
        var sourceType = CastExpressionHelper.GetSourceTypeAt(sourceTypes, argIndex) ?? elementType;
        return CastExpressionHelper.GenerateCast(sourceType, elementType, argExpression, compilation);
    }
}
