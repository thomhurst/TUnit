using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core.Converters;
using TUnit.Core.Interfaces.SourceGenerator;

namespace TUnit.Core.Helpers;

public static class CastHelper
{
    [UnconditionalSuppressMessage("Trimming", "IL2072", 
        Justification = "Type conversion uses DynamicallyAccessedMembers for known conversion patterns. For AOT scenarios, use explicit type conversions.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", 
        Justification = "Reflection-based conversion is a fallback for runtime scenarios. AOT applications should use explicit conversions.")]
    public static T? Cast<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(object? value)
    {
        if (value is null)
        {
            return default(T?);
        }

        if (value is T successfulCast)
        {
            return successfulCast;
        }

        var underlyingType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

        if (value.GetType().IsAssignableTo(underlyingType))
        {
            return (T) value;
        }
        
        // Try AOT converter registry first
        if (AotConverterRegistry.TryConvert(value.GetType(), underlyingType, value, out var converted))
        {
            return (T?) converted;
        }

        if (value is not string
            && value is IEnumerable enumerable
            && !value.GetType().IsArray  // Don't unwrap arrays
            && !typeof(IEnumerable).IsAssignableFrom(typeof(T)))
        {
            // Special handling for CustomAttributeTypedArgument collections in .NET Framework
            var typeName = value.GetType().FullName;
            if (typeName != null && typeName.Contains("CustomAttributeTypedArgument"))
            {
                // For ReadOnlyCollection<CustomAttributeTypedArgument>, we need to extract the actual values
                var firstItem = enumerable.Cast<object>().FirstOrDefault();
                if (firstItem != null)
                {
                    // Use reflection to get the Value property
                    var valueProperty = GetValuePropertySafe(firstItem.GetType());
                    if (valueProperty != null)
                    {
                        value = valueProperty.GetValue(firstItem);
                    }
                    else
                    {
                        value = firstItem;
                    }
                }
                else
                {
                    value = null;
                }
            }
            else
            {
                value = enumerable.Cast<object>().ElementAtOrDefault(0);
            }
        }

        if (underlyingType.IsEnum)
        {
            return (T?) Enum.ToObject(underlyingType, value!);
        }

        // Special handling for array types - check this before IConvertible
        if (underlyingType.IsArray)
        {
            var targetElementType = underlyingType.GetElementType()!;
            
            // Handle null -> empty array
            if (value is null)
            {
                return (T?)(object)Array.CreateInstance(targetElementType, 0);
            }
            
            // Handle single value -> single element array
            if (!value.GetType().IsArray)
            {
                if (value is IConvertible)
                {
                    try
                    {
                        var convertedValue = Convert.ChangeType(value, targetElementType);
                        var array = Array.CreateInstance(targetElementType, 1);
                        array.SetValue(convertedValue, 0);
                        return (T?)(object)array;
                    }
                    catch
                    {
                        // If direct conversion fails, continue with other approaches
                    }
                }
            }
            // Handle array -> array with element type conversion
            else if (value is Array sourceArray)
            {
                var sourceElementType = value.GetType().GetElementType()!;
                
                // If element types match, return as-is
                if (sourceElementType == targetElementType)
                {
                    return (T?)value;
                }
                
                // Otherwise, convert each element
                try
                {
                    var targetArray = Array.CreateInstance(targetElementType, sourceArray.Length);
                    for (int i = 0; i < sourceArray.Length; i++)
                    {
                        var sourceElement = sourceArray.GetValue(i);
                        var convertedElement = sourceElement is IConvertible 
                            ? Convert.ChangeType(sourceElement, targetElementType)
                            : sourceElement;
                        targetArray.SetValue(convertedElement, i);
                    }
                    return (T?)(object)targetArray;
                }
                catch
                {
                    // If conversion fails, continue with other approaches
                }
            }
        }

        var conversionMethod = GetConversionMethod(value!.GetType(), underlyingType);

        if (conversionMethod is null && value is IConvertible)
        {
            return (T?) Convert.ChangeType(value, underlyingType);
        }

        if (conversionMethod is null)
        {
            // Check if we can do unboxing directly for value types
            if (underlyingType.IsValueType && value.GetType() == typeof(object))
            {
                try
                {
                    return (T)value;
                }
                catch
                {
                    // If unboxing fails, continue with the original approach
                }
            }
            return (T?) value;
        }

        // Even in source generation mode, we use reflection as a fallback for custom conversions
        // The AOT analyzer will warn about incompatibility at compile time
        try
        {
            return (T?) conversionMethod.Invoke(null, [value]);
        }
        catch (Exception ex) when (ex is NotSupportedException || ex is InvalidOperationException)
        {
            // In AOT scenarios, reflection invoke might fail
            // Try a direct cast as a last resort
            try
            {
                return (T)value;
            }
            catch
            {
                // If all else fails, return the value as-is and let the runtime handle it
                return (T?)value;
            }
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2072", 
        Justification = "Type conversion uses DynamicallyAccessedMembers for known conversion patterns. For AOT scenarios, use explicit type conversions.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", 
        Justification = "Reflection-based conversion is a fallback for runtime scenarios. AOT applications should use explicit conversions.")]
    public static object? Cast([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type, object? value)
    {
        if (value is null)
        {
            return null;
        }

        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        if (value.GetType().IsAssignableTo(underlyingType))
        {
            return value;
        }

        // Try AOT converter registry first
        if (AotConverterRegistry.TryConvert(value.GetType(), underlyingType, value, out var converted))
        {
            return converted;
        }

        if (type.IsGenericParameter)
        {
            return value;
        }

        if (value is not string
            && value is IEnumerable enumerable
            && !value.GetType().IsArray  // Don't unwrap arrays
            && !typeof(IEnumerable).IsAssignableFrom(type))
        {
            // Special handling for CustomAttributeTypedArgument collections in .NET Framework
            var typeName = value.GetType().FullName;
            if (typeName != null && typeName.Contains("CustomAttributeTypedArgument"))
            {
                // For ReadOnlyCollection<CustomAttributeTypedArgument>, we need to extract the actual values
                var firstItem = enumerable.Cast<object>().FirstOrDefault();
                if (firstItem != null)
                {
                    // Use reflection to get the Value property
                    var valueProperty = GetValuePropertySafe(firstItem.GetType());
                    if (valueProperty != null)
                    {
                        value = valueProperty.GetValue(firstItem);
                    }
                    else
                    {
                        value = firstItem;
                    }
                }
                else
                {
                    value = null;
                }
            }
            else
            {
                value = enumerable.Cast<object>().ElementAtOrDefault(0);
            }
        }

        if (underlyingType.IsEnum)
        {
            return Enum.ToObject(underlyingType, value!);
        }

        // Special handling for array types - check this before IConvertible
        if (underlyingType.IsArray)
        {
            var targetElementType = underlyingType.GetElementType()!;
            
            // Handle null -> empty array
            if (value is null)
            {
                return Array.CreateInstance(targetElementType, 0);
            }
            
            // Handle single value -> single element array
            if (!value.GetType().IsArray)
            {
                if (value is IConvertible)
                {
                    try
                    {
                        var convertedValue = Convert.ChangeType(value, targetElementType);
                        var array = Array.CreateInstance(targetElementType, 1);
                        array.SetValue(convertedValue, 0);
                        return array;
                    }
                    catch
                    {
                        // If direct conversion fails, continue with other approaches
                    }
                }
            }
            // Handle array -> array with element type conversion
            else if (value is Array sourceArray)
            {
                var sourceElementType = value.GetType().GetElementType()!;
                
                // If element types match, return as-is
                if (sourceElementType == targetElementType)
                {
                    return value;
                }
                
                // Otherwise, convert each element
                try
                {
                    var targetArray = Array.CreateInstance(targetElementType, sourceArray.Length);
                    for (int i = 0; i < sourceArray.Length; i++)
                    {
                        var sourceElement = sourceArray.GetValue(i);
                        var convertedElement = sourceElement is IConvertible 
                            ? Convert.ChangeType(sourceElement, targetElementType)
                            : sourceElement;
                        targetArray.SetValue(convertedElement, i);
                    }
                    return targetArray;
                }
                catch
                {
                    // If conversion fails, continue with other approaches
                }
            }
        }

        var conversionMethod = GetConversionMethod(value!.GetType(), underlyingType);

        if (conversionMethod is null && value is IConvertible)
        {
            return Convert.ChangeType(value, underlyingType);
        }

        if (conversionMethod is null)
        {
            // Check if we can do unboxing directly for value types
            if (underlyingType.IsValueType && value.GetType() == typeof(object))
            {
                try
                {
                    return value;
                }
                catch
                {
                    // If unboxing fails, continue with the original approach
                }
            }
            return value;
        }

        // Even in source generation mode, we use reflection as a fallback for custom conversions
        // The AOT analyzer will warn about incompatibility at compile time
        return conversionMethod.Invoke(null, [value]);
    }

    public static MethodInfo? GetConversionMethod([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type baseType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type targetType)
    {
        var methods = baseType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Concat(targetType.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .ToArray();

        return methods
                   .FirstOrDefault(mi =>
                       mi.Name == "op_Implicit" && mi.ReturnType == targetType && HasCorrectInputType(baseType, mi))
               ?? methods
                   .FirstOrDefault(mi =>
                       mi.Name == "op_Explicit" && mi.ReturnType == targetType && HasCorrectInputType(baseType, mi));
    }

    private static bool HasCorrectInputType(Type baseType, MethodInfo mi)
    {
        var pi = mi.GetParameters().FirstOrDefault();
        return pi != null && pi.ParameterType == baseType;
    }
    
    /// <summary>
    /// Gets the "Value" property from a type in an AOT-safer manner.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2075:Target method return value does not satisfy annotation requirements",
        Justification = "Value property access is used for unwrapping CustomAttributeTypedArgument. For AOT scenarios, use source-generated attribute discovery.")]
    private static PropertyInfo? GetValuePropertySafe([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
    {
        return type.GetProperty("Value");
    }
    
}
