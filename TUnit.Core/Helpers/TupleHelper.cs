using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TUnit.Core.Helpers;

/// <summary>
/// Helper methods for working with tuple types at runtime
/// </summary>
public static class TupleHelper
{
    /// <summary>
    /// Checks if a type is a tuple type (ValueTuple or Tuple)
    /// </summary>
    public static bool IsTupleType(Type type)
    {
        if (type == null)
        {
            return false;
        }

        return type.IsGenericType && (
            type.FullName?.StartsWith("System.ValueTuple`") == true ||
            type.FullName?.StartsWith("System.Tuple`") == true
        );
    }
    
    /// <summary>
    /// Unwraps a tuple into an array of its elements
    /// </summary>
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:UnrecognizedReflectionPattern",
        Justification = "This method specifically handles tuple types which have known structure")]
    public static object?[] UnwrapTuple(object? value)
    {
        if (value == null)
        {
            return [null];
        }

        var type = value.GetType();
        if (!IsTupleType(type))
        {
            // Not a tuple, return as single-element array
            return [value];
        }
        
        var elements = new List<object?>();
        
        if (type.FullName?.StartsWith("System.ValueTuple`") == true)
        {
            // Handle ValueTuple - access via fields
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields.OrderBy(f => f.Name))
            {
                elements.Add(field.GetValue(value));
            }
        }
        else if (type.FullName?.StartsWith("System.Tuple`") == true)
        {
            // Handle Tuple - access via properties
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.Name.StartsWith("Item"))
                .OrderBy(p => p.Name);
            
            foreach (var property in properties)
            {
                elements.Add(property.GetValue(value));
            }
        }
        
        return elements.ToArray();
    }
    
    /// <summary>
    /// Checks if a value should be unwrapped as a tuple for method arguments
    /// </summary>
    public static bool ShouldUnwrapForMethodArguments(object? value, int expectedParameterCount)
    {
        if (value == null || expectedParameterCount <= 0)
        {
            return false;
        }

        var type = value.GetType();
        if (!IsTupleType(type))
        {
            return false;
        }

        // Only unwrap if we have multiple parameters expected
        // and the value is a single tuple (not already an array)
        return expectedParameterCount > 1 && !type.IsArray;
    }
    
    /// <summary>
    /// Checks if a type is an array of tuples
    /// </summary>
    public static bool IsTupleArrayType(Type type)
    {
        if (type == null || !type.IsArray)
        {
            return false;
        }

        var elementType = type.GetElementType();
        return elementType != null && IsTupleType(elementType);
    }
    
    /// <summary>
    /// Expands an array of tuples into individual tuple elements for data source generation
    /// For example: [(1, "a"), (2, "b")] becomes individual items that each unwrap to [1, "a"] and [2, "b"]
    /// </summary>
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:UnrecognizedReflectionPattern",
        Justification = "This method specifically handles tuple types which have known structure")]
    public static IEnumerable<object?[]> ExpandTupleArray(object? value)
    {
        if (value == null)
        {
            yield break;
        }

        var type = value.GetType();
        if (!IsTupleArrayType(type))
        {
            yield break;
        }

        if (value is Array array)
        {
            foreach (var item in array)
            {
                yield return UnwrapTuple(item);
            }
        }
    }
}