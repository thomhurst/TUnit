using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace TUnit.Core.Helpers;

/// <summary>
/// Provides AOT-safe tuple creation without reflection for common tuple types.
/// </summary>
public static class TupleFactory
{
    private static readonly Dictionary<Type, Func<object?[], object?>> TypedFactories = new();
    
    static TupleFactory()
    {
        // Register factories for common tuple types with object elements
        RegisterFactory<ValueTuple<object?, object?>>((args) => 
            new ValueTuple<object?, object?>(args[0], args[1]));
        RegisterFactory<ValueTuple<object?, object?, object?>>((args) => 
            new ValueTuple<object?, object?, object?>(args[0], args[1], args[2]));
        RegisterFactory<ValueTuple<object?, object?, object?, object?>>((args) => 
            new ValueTuple<object?, object?, object?, object?>(args[0], args[1], args[2], args[3]));
        RegisterFactory<ValueTuple<object?, object?, object?, object?, object?>>((args) => 
            new ValueTuple<object?, object?, object?, object?, object?>(args[0], args[1], args[2], args[3], args[4]));
        RegisterFactory<ValueTuple<object?, object?, object?, object?, object?, object?>>((args) => 
            new ValueTuple<object?, object?, object?, object?, object?, object?>(args[0], args[1], args[2], args[3], args[4], args[5]));
        RegisterFactory<ValueTuple<object?, object?, object?, object?, object?, object?, object?>>((args) => 
            new ValueTuple<object?, object?, object?, object?, object?, object?, object?>(args[0], args[1], args[2], args[3], args[4], args[5], args[6]));
    }
    
    private static void RegisterFactory<T>(Func<object?[], T> factory) where T : struct
    {
        TypedFactories[typeof(T)] = args => factory(args);
    }
    
    /// <summary>
    /// Creates a tuple from the given elements in an AOT-safe manner when possible.
    /// Falls back to returning the first element for unsupported scenarios.
    /// </summary>
    /// <param name="tupleType">The type of tuple to create</param>
    /// <param name="elements">The elements to include in the tuple</param>
    /// <returns>The created tuple, or the first element if creation fails</returns>
    public static object? CreateTuple([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type tupleType, object?[] elements)
    {
        if (elements == null || elements.Length == 0)
        {
            return null;
        }
        
        // Try typed factory first (for object tuples)
        if (TypedFactories.TryGetValue(tupleType, out var factory))
        {
            try
            {
                return factory(elements);
            }
            catch
            {
                // Fall through to other approaches
            }
        }
        
        // Try to use reflection to create the tuple with proper types
        // This is necessary because the tuple type has specific generic arguments
        if (IsTupleType(tupleType) && tupleType.IsGenericType)
        {
            try
            {
                // For AOT scenarios, we'll try to use Activator.CreateInstance
                // This requires the tuple type to have DynamicallyAccessedMembers attribute
                var result = Activator.CreateInstance(tupleType, elements);
                if (result != null)
                {
                    return result;
                }
            }
            catch
            {
                // Fall through to object tuple creation
            }
        }
        
        // Try generic factory based on element count - creates object tuples
        if (IsTupleType(tupleType))
        {
            try
            {
                return elements.Length switch
                {
                    1 => new ValueTuple<object?>(elements[0]),
                    2 => (elements[0], elements[1]),
                    3 => (elements[0], elements[1], elements[2]),
                    4 => (elements[0], elements[1], elements[2], elements[3]),
                    5 => (elements[0], elements[1], elements[2], elements[3], elements[4]),
                    6 => (elements[0], elements[1], elements[2], elements[3], elements[4], elements[5]),
                    7 => (elements[0], elements[1], elements[2], elements[3], elements[4], elements[5], elements[6]),
                    // For tuples with more than 7 elements, we can't handle them in an AOT-safe way
                    // Return the first element as a fallback
                    _ => elements.FirstOrDefault()
                };
            }
            catch
            {
                // If tuple creation fails, return the first element
                return elements.FirstOrDefault();
            }
        }
        
        // For non-tuple types or unknown patterns, return the first element
        return elements.FirstOrDefault();
    }
    
    /// <summary>
    /// Checks if a type is a ValueTuple type.
    /// </summary>
    public static bool IsTupleType(Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition().FullName?.StartsWith("System.ValueTuple") == true;
    }
    
    /// <summary>
    /// Tries to create a tuple using reflection. Only call this from contexts that are already marked with RequiresUnreferencedCode.
    /// </summary>
    /// <param name="tupleType">The tuple type to create</param>
    /// <param name="elements">The elements for the tuple</param>
    /// <param name="result">The created tuple if successful</param>
    /// <returns>True if the tuple was created successfully</returns>
    public static bool TryCreateTupleUsingReflection([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type tupleType, object?[] elements, out object? result)
    {
        result = null;
        
        // This method should only be called from contexts that are already handling reflection
        // and have the appropriate RequiresUnreferencedCode attributes
        if (!tupleType.IsGenericType || !tupleType.GetGenericTypeDefinition().FullName?.StartsWith("System.ValueTuple") == true)
        {
            return false;
        }
        
        try
        {
            result = Activator.CreateInstance(tupleType, elements);
            return true;
        }
        catch
        {
            return false;
        }
    }
}