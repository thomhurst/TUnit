using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace TUnit.Core.Helpers;

/// <summary>
/// Helper methods for working with tuple types at runtime
/// </summary>
public static class TupleHelper
{

#if NET
    /// <summary>
    /// Checks if a type is a tuple type (ValueTuple or Tuple)
    /// </summary>
    public static bool IsTupleType(ITuple tuple)
    {
        return true;
    }
#endif

    /// <summary>
    /// Checks if a type is a tuple type (ValueTuple or Tuple)
    /// </summary>
    public static bool IsTupleType(object? obj)
    {
#if NET
        return obj is ITuple;
#else
        return obj != null && IsTupleType(obj.GetType());
#endif
    }

    /// <summary>
    /// Checks if a type is a tuple type (ValueTuple or Tuple)
    /// </summary>
    public static bool IsTupleType(Type? type)
    {
        if (type == null)
        {
            return false;
        }

#if NET
        return type.IsAssignableTo(typeof(ITuple));
#else
        return type.IsGenericType && (
            type.FullName?.StartsWith("System.ValueTuple`") == true ||
            type.FullName?.StartsWith("System.Tuple`") == true
        );
#endif
    }

#if NET
    /// <summary>
    /// Unwraps a tuple into an array of its elements
    /// </summary>
    public static object?[] UnwrapTuple(ITuple? value)
    {
        if (value == null)
        {
            return [null];
        }

        var elements = new object?[value.Length];

        for (var i = 0; i < value.Length; i++)
        {
            elements[i] = value[i];
        }

        return elements;
    }
#endif

    /// <summary>
    /// Unwraps a tuple into an array of its elements
    /// </summary>
    public static object?[] UnwrapTuple(object? value)
    {
#if NET
        return UnwrapTuple(value as ITuple);
#else
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

        // Even in source generation mode, we use reflection as a fallback
        // The AOT analyzer will warn about incompatibility at compile time
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
#endif
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
    #if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Tuple expansion uses reflection as fallback")]
    #endif
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
