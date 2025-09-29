using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TUnit.Engine.Helpers;

[UnconditionalSuppressMessage("Trimming", "IL2075:\'this\' argument does not satisfy \'DynamicallyAccessedMembersAttribute\' in call to target method. The return value of the source method does not have matching annotations.")]
internal class TupleHelper
{
    // Cache reflection metadata to avoid repeated reflection calls
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _tuplePropertyCache = new();
    private static readonly ConcurrentDictionary<Type, FieldInfo[]> _valueTupleFieldCache = new();
    
    public static bool TryParseTupleToObjectArray(object? tuple, [NotNullWhen(true)] out object?[]? objectArray)
    {
        objectArray = null;

        if (tuple == null)
        {
            return false;
        }

        var type = tuple.GetType();

        if (type.IsGenericType && type.FullName!.StartsWith("System.Tuple"))
        {
            // Handle Tuple recursively with pre-sized list
            var result = new List<object?>(8); // Most tuples have <= 8 items
            FlattenTuple(tuple, result);
            objectArray = result.ToArray();

            return true;
        }

        if (type.IsValueType && type.FullName!.StartsWith("System.ValueTuple"))
        {
            // Handle ValueTuple recursively with pre-sized list
            var result = new List<object?>(8); // Most tuples have <= 8 items
            FlattenValueTuple(tuple, result);
            objectArray = result.ToArray();

            return true;
        }

        return false;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Tuple reflection is handled by the calling method's suppressions")]
    [UnconditionalSuppressMessage("Trimming", "IL2111:Method with parameters or return value with 'DynamicallyAccessedMembersAttribute' is accessed via reflection", Justification = "Tuple type analysis is controlled at the entry point")]
    private static void FlattenTuple(object tuple, List<object?> result)
    {
        var type = tuple.GetType();

        // Get cached properties or compute and cache them
        var properties = _tuplePropertyCache.GetOrAdd(type, GetTupleProperties);

        // Process items 1 to 7 (or fewer if the tuple is smaller)
        var propertyCount = properties.Length;
        var itemCount = propertyCount == 8 ? 7 : propertyCount; // Handle Rest property separately
        
        for (var i = 0; i < itemCount; i++)
        {
            result.Add(properties[i].GetValue(tuple));
        }

        // Check if we have a Rest property (8th item in a large tuple)
        if (propertyCount == 8)
        {
            var rest = properties[7].GetValue(tuple);

            if (rest != null && rest.GetType().FullName!.StartsWith("System.Tuple"))
            {
                FlattenTuple(rest, result);
            }
            else
            {
                result.Add(rest);
            }
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "ValueTuple reflection is handled by the calling method's suppressions")]
    [UnconditionalSuppressMessage("Trimming", "IL2111:Method with parameters or return value with 'DynamicallyAccessedMembersAttribute' is accessed via reflection", Justification = "ValueTuple type analysis is controlled at the entry point")]
    private static void FlattenValueTuple(object tuple, List<object?> result)
    {
        var type = tuple.GetType();

        // Get cached fields or compute and cache them
        var fields = _valueTupleFieldCache.GetOrAdd(type, GetValueTupleFields);

        // Process items 1 to 7 (or fewer if the tuple is smaller)
        for (var i = 0; i < fields.Length; i++)
        {
            var field = fields[i];
            
            // Skip the last field if it's called Rest
            if (i == 7 || (i == fields.Length - 1 && field.Name == "Rest"))
            {
                var rest = field.GetValue(tuple);

                if (rest != null && rest.GetType().IsValueType && rest.GetType().FullName!.StartsWith("System.ValueTuple"))
                {
                    FlattenValueTuple(rest, result);
                }
                else
                {
                    result.Add(rest);
                }

                break;
            }

            result.Add(field.GetValue(tuple));
        }
    }
    
    /// <summary>
    /// Get tuple properties with caching
    /// </summary>
    private static PropertyInfo[] GetTupleProperties([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
    {
        var allProperties = type.GetProperties();
        var itemProperties = new List<PropertyInfo>(8);
        
        // Pre-filter and sort Item properties without LINQ
        for (var i = 0; i < allProperties.Length; i++)
        {
            var prop = allProperties[i];
            if (prop.Name.StartsWith("Item"))
            {
                itemProperties.Add(prop);
            }
        }
        
        // Sort properties by name (Item1, Item2, etc.) manually for better performance
        itemProperties.Sort((x, y) => string.CompareOrdinal(x.Name, y.Name));
        return itemProperties.ToArray();
    }
    
    /// <summary>
    /// Get value tuple fields with caching
    /// </summary>
    private static FieldInfo[] GetValueTupleFields([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] Type type)
    {
        var allFields = type.GetFields();
        
        // Sort fields manually for consistent ordering without LINQ
        Array.Sort(allFields, (x, y) => string.CompareOrdinal(x.Name, y.Name));
        return allFields;
    }

    /// <summary>
    /// Clear the reflection cache if needed (for testing or memory management)
    /// </summary>
    internal static void ClearCache()
    {
        _tuplePropertyCache.Clear();
        _valueTupleFieldCache.Clear();
    }
}

