using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Helpers;

/// <summary>
/// Helper for generating tuple-aware argument access code in source generators.
/// Handles unwrapping tuple types to access individual tuple items.
/// </summary>
public static class TupleArgumentHelper
{
    /// <summary>
    /// Generates code to access an argument that may be a tuple.
    /// If the parameter type is a tuple, generates args[index].Item1, args[index].Item2, etc.
    /// Otherwise, generates standard args[index] access.
    /// </summary>
    /// <param name="parameterType">The type of the parameter</param>
    /// <param name="argumentsArrayName">The name of the arguments array (e.g., "args")</param>
    /// <param name="baseIndex">The base index in the arguments array</param>
    /// <returns>A list of argument access expressions</returns>
    public static List<string> GenerateArgumentAccess(ITypeSymbol parameterType, string argumentsArrayName, int baseIndex)
    {
        var argumentExpressions = new List<string>();
        
        if (IsTupleType(parameterType))
        {
            var tupleElements = GetTupleElements(parameterType);
            for (int i = 0; i < tupleElements.Count; i++)
            {
                var tupleElement = tupleElements[i];
                var itemProperty = $"Item{i + 1}";
                var castExpression = $"TUnit.Core.Helpers.CastHelper.Cast<{tupleElement.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>({argumentsArrayName}[{baseIndex}].{itemProperty})";
                argumentExpressions.Add(castExpression);
            }
        }
        else
        {
            var castExpression = $"TUnit.Core.Helpers.CastHelper.Cast<{parameterType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>({argumentsArrayName}[{baseIndex}])";
            argumentExpressions.Add(castExpression);
        }
        
        return argumentExpressions;
    }

    /// <summary>
    /// Generates code to access constructor arguments.
    /// Since data sources already unwrap tuples into individual arguments,
    /// we simply access each argument by index.
    /// </summary>
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
            var castExpression = $"TUnit.Core.Helpers.CastHelper.Cast<{parameterType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>({argumentsArrayName}[{i}])";
            argumentExpressions.Add(castExpression);
        }
        
        return argumentExpressions;
    }

    /// <summary>
    /// Generates method invocation arguments, handling tuple unwrapping.
    /// </summary>
    /// <param name="parameters">The method parameters</param>
    /// <param name="argumentsArrayName">The name of the arguments array</param>
    /// <returns>Comma-separated argument expressions for method invocation</returns>
    public static string GenerateMethodInvocationArguments(IList<IParameterSymbol> parameters, string argumentsArrayName)
    {
        var allArguments = new List<string>();
        var argumentIndex = 0;
        
        foreach (var parameter in parameters)
        {
            if (IsTupleType(parameter.Type))
            {
                var tupleElements = GetTupleElements(parameter.Type);
                for (int i = 0; i < tupleElements.Count; i++)
                {
                    var tupleElement = tupleElements[i];
                    var itemProperty = $"Item{i + 1}";
                    var castExpression = $"TUnit.Core.Helpers.CastHelper.Cast<{tupleElement.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>({argumentsArrayName}[{argumentIndex}].{itemProperty})";
                    allArguments.Add(castExpression);
                }
                argumentIndex++;
            }
            else
            {
                var castExpression = $"TUnit.Core.Helpers.CastHelper.Cast<{parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>({argumentsArrayName}[{argumentIndex}])";
                allArguments.Add(castExpression);
                argumentIndex++;
            }
        }
        
        return string.Join(", ", allArguments);
    }

    /// <summary>
    /// Checks if a type is a tuple type (ValueTuple).
    /// </summary>
    private static bool IsTupleType(ITypeSymbol type)
    {
        if (type is not INamedTypeSymbol namedType)
            return false;
            
        return namedType.IsTupleType || 
               (namedType.IsGenericType && namedType.ConstructedFrom.Name.StartsWith("ValueTuple"));
    }

    /// <summary>
    /// Gets the individual element types from a tuple type.
    /// </summary>
    private static List<ITypeSymbol> GetTupleElements(ITypeSymbol tupleType)
    {
        if (tupleType is INamedTypeSymbol { IsTupleType: true } namedType)
        {
            return namedType.TupleElements.IsDefault ?
                [
                ]
                : namedType.TupleElements.Select(e => e.Type).ToList();
        }
        
        if (tupleType is INamedTypeSymbol { IsGenericType: true } genericType && genericType.ConstructedFrom.Name.StartsWith("ValueTuple"))
        {
            return genericType.TypeArguments.ToList();
        }
        
        return
        [
        ];
    }
}