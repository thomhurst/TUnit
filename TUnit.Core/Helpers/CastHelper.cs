using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using TUnit.Core.Converters;

namespace TUnit.Core.Helpers;

[UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.")]
[UnconditionalSuppressMessage("Trimming", "IL2072:Target parameter argument does not satisfy \'DynamicallyAccessedMembersAttribute\' in call to target method. The return value of the source method does not have matching annotations.")]
public static class CastHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.",
        Justification = "Array.CreateInstance is used for test data generation at discovery time, not in AOT-compiled test execution.")]
    public static T? Cast<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] T>(object? value)
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

        if (value is IConvertible && underlyingType.IsPrimitive)
        {
            try
            {
                return (T?) Convert.ChangeType(value, underlyingType);
            }
            catch
            {
                // If direct conversion fails, continue with other approaches
            }
        }

        if (value is not string
            && value is IEnumerable enumerable
            && !value.GetType().IsArray  // Don't unwrap arrays
            && !typeof(IEnumerable).IsAssignableFrom(typeof(T)))
        {
            value = enumerable.Cast<object>().ElementAtOrDefault(0);
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
                ThrowOnAot(value, underlyingType);
                return (T?)(object)Array.CreateInstance(targetElementType, 0);
            }

            // Handle single value -> single element array
            if (!value.GetType().IsArray)
            {
                if (value is IConvertible)
                {
                    ThrowOnAot(value, underlyingType);

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
                    ThrowOnAot(value, underlyingType);
                    var targetArray = Array.CreateInstance(targetElementType, sourceArray.Length);
                    for (var i = 0; i < sourceArray.Length; i++)
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

            // Log diagnostic information for debugging single file mode issues
            if (Environment.GetEnvironmentVariable("TUNIT_DIAGNOSTIC_CAST") == "true")
            {
                Console.WriteLine($"[CastHelper] No conversion found from {value.GetType().FullName} to {underlyingType.FullName}");
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.",
        Justification = "Array.CreateInstance is used for test data generation at discovery time, not in AOT-compiled test execution.")]
    public static object? Cast([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type type, object? value)
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
                    ThrowOnAot(value, underlyingType);
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
                ThrowOnAot(value, underlyingType);
                return Array.CreateInstance(targetElementType, 0);
            }

            // Handle single value -> single element array
            if (!value.GetType().IsArray)
            {
                if (value is IConvertible)
                {
                    ThrowOnAot(value, underlyingType);
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
                    ThrowOnAot(value, underlyingType);
                    var targetArray = Array.CreateInstance(targetElementType, sourceArray.Length);
                    for (var i = 0; i < sourceArray.Length; i++)
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

    public static MethodInfo? GetConversionMethod([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type baseType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type targetType)
    {
        var baseMethods = baseType.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Concat(baseType.GetMethods(BindingFlags.Public | BindingFlags.Static));

        var targetMethods = targetType.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Concat(targetType.GetMethods(BindingFlags.Public | BindingFlags.Static));

        var methods = baseMethods.Concat(targetMethods).Distinct().ToArray();

        // Look for implicit conversion first
        var implicitMethod = methods
            .FirstOrDefault(mi =>
                mi.Name == "op_Implicit" && mi.ReturnType == targetType && HasCorrectInputType(baseType, mi));

        if (implicitMethod != null)
        {
            return implicitMethod;
        }

        // Then look for explicit conversion
        return methods
            .FirstOrDefault(mi =>
                mi.Name == "op_Explicit" && mi.ReturnType == targetType && HasCorrectInputType(baseType, mi));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ThrowOnAot(object? value, Type? targetType)
    {
#if NET
        if (!RuntimeFeature.IsDynamicCodeSupported)
        {
            throw new InvalidOperationException(
                $"Cannot cast {value?.GetType()?.Name ?? "null"} to {targetType?.Name} in AOT mode. " +
                "Consider using AotConverterRegistry.Register() for custom type conversions.");
        }
#endif
    }

    private static bool HasCorrectInputType(Type baseType, MethodInfo mi)
    {
        var pi = mi.GetParameters().FirstOrDefault();
        return pi != null && pi.ParameterType == baseType;
    }

    /// <summary>
    /// Gets the "Value" property from a type in an AOT-safer manner.
    /// </summary>
    private static PropertyInfo? GetValuePropertySafe([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
    {
        return type.GetProperty("Value");
    }

}
