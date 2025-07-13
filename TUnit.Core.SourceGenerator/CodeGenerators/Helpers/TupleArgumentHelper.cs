using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

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
    /// Generates code to access constructor arguments that may include tuples.
    /// Returns expressions for accessing each individual argument.
    /// Handles the case where ClassDataSource returns a single tuple containing all arguments.
    /// </summary>
    /// <param name="parameterTypes">The types of all constructor parameters</param>
    /// <param name="argumentsArrayName">The name of the arguments array (e.g., "args")</param>
    /// <returns>A list of argument access expressions for the constructor</returns>
    public static List<string> GenerateConstructorArgumentAccess(IList<ITypeSymbol> parameterTypes, string argumentsArrayName)
    {
        var argumentExpressions = new List<string>();
        
        // For multiple parameters from ClassDataSource, assume args[0] contains a tuple
        if (parameterTypes.Count > 1)
        {
            // Generate cast to tuple type and access individual items
            var tupleTypeElements = string.Join(", ", parameterTypes.Select(t => t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
            var tupleTypeName = $"({tupleTypeElements})";
            
            // Generate conditional access - if args.Length == 1, treat as tuple, otherwise as individual args
            for (int i = 0; i < parameterTypes.Count; i++)
            {
                var parameterType = parameterTypes[i];
                var itemProperty = $"Item{i + 1}";
                
                // Generate code that checks args length and handles both cases
                var expression = $"({argumentsArrayName}.Length == 1 ? " +
                    $"TUnit.Core.Helpers.CastHelper.Cast<{tupleTypeName}>({argumentsArrayName}[0]).{itemProperty} : " +
                    $"TUnit.Core.Helpers.CastHelper.Cast<{parameterType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>({argumentsArrayName}[{i}]))";
                
                argumentExpressions.Add(expression);
            }
            
            return argumentExpressions;
        }
        
        // For single parameter, use standard approach
        var argumentIndex = 0;
        foreach (var parameterType in parameterTypes)
        {
            if (IsTupleType(parameterType))
            {
                var tupleElements = GetTupleElements(parameterType);
                for (int i = 0; i < tupleElements.Count; i++)
                {
                    var tupleElement = tupleElements[i];
                    var itemProperty = $"Item{i + 1}";
                    var castExpression = $"TUnit.Core.Helpers.CastHelper.Cast<{tupleElement.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>({argumentsArrayName}[{argumentIndex}].{itemProperty})";
                    argumentExpressions.Add(castExpression);
                }
                argumentIndex++;
            }
            else
            {
                var castExpression = $"TUnit.Core.Helpers.CastHelper.Cast<{parameterType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>({argumentsArrayName}[{argumentIndex}])";
                argumentExpressions.Add(castExpression);
                argumentIndex++;
            }
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
        if (tupleType is INamedTypeSymbol namedType && namedType.IsTupleType)
        {
            return namedType.TupleElements.IsDefault ? new List<ITypeSymbol>() : namedType.TupleElements.Select(e => e.Type).ToList();
        }
        
        if (tupleType is INamedTypeSymbol genericType && genericType.IsGenericType && genericType.ConstructedFrom.Name.StartsWith("ValueTuple"))
        {
            return genericType.TypeArguments.ToList();
        }
        
        return new List<ITypeSymbol>();
    }
}