using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using TUnit.Core.Converters;

namespace TUnit.Core.Helpers;

public static class CastHelper
{
    // Cache for conversion methods to avoid repeated reflection lookups
    private static readonly ConcurrentDictionary<(Type Source, Type Target), MethodInfo?> ConversionMethodCache = new();

    /// <summary>
    /// Attempts to cast or convert a value to the specified type T.
    /// Uses a layered approach: fast paths first (AOT-safe), then reflection fallbacks.
    /// </summary>
    public static T? Cast<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>(object? value)
    {
        if (value is T t)
        {
            return t;
        }

        return (T?)Cast(typeof(T), value);
    }

    /// <summary>
    /// Attempts to cast or convert a value to the specified type.
    /// Conversion priority:
    /// 1. Fast path: null handling, direct cast, nullable unwrapping
    /// 2. AOT-safe: AotConverterRegistry, primitives, enums
    /// 3. Reflection fallback: custom operators, arrays (throws in AOT)
    /// </summary>
    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.")]
    public static object? Cast([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type type, object? value)
    {
        // Fast path: handle null
        if (value is null)
        {
            return type.IsValueType && Nullable.GetUnderlyingType(type) == null
                ? Activator.CreateInstance(type) // default(T) for non-nullable value types
                : null;
        }

        var targetType = Nullable.GetUnderlyingType(type) ?? type;
        var sourceType = value.GetType();

        // Fast path: direct cast if types are assignable
        if (sourceType.IsAssignableTo(targetType))
        {
            return value;
        }

        // Fast path: generic parameter types (can't convert)
        if (type.IsGenericParameter)
        {
            return value;
        }

        // Layer 1: AOT-safe conversions (no reflection)
        if (TryAotSafeConversion(targetType, sourceType, value, out var result))
        {
            return result;
        }

        // Layer 2: Reflection-based conversions (not AOT-compatible)
        if (TryReflectionConversion(targetType, sourceType, value, out result))
        {
            return result;
        }

        // Last resort: return value as-is and hope for the best
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryAotSafeConversion(Type targetType, Type sourceType, object value, out object? result)
    {
        // Try AOT converter registry first
        if (AotConverterRegistry.TryConvert(sourceType, targetType, value, out result))
        {
            return true;
        }

        // Handle IConvertible primitives (int, string, double, etc.)
        if (value is IConvertible && targetType.IsPrimitive)
        {
            try
            {
                result = Convert.ChangeType(value, targetType);
                return true;
            }
            catch
            {
                // Conversion failed, continue with other strategies
            }
        }

        // Handle enum conversions
        if (targetType.IsEnum)
        {
            try
            {
                result = Enum.ToObject(targetType, value);
                return true;
            }
            catch
            {
                // Conversion failed
            }
        }

        // Unwrap single-element enumerables (but not strings or arrays)
        if (value is not string && !sourceType.IsArray && value is IEnumerable enumerable && !typeof(IEnumerable).IsAssignableFrom(targetType))
        {
            var firstElement = enumerable.Cast<object>().FirstOrDefault();
            if (firstElement != null)
            {
                // Recursively try to cast the first element
                return TryAotSafeConversion(targetType, firstElement.GetType(), firstElement, out result);
            }
        }

        result = null;
        return false;
    }

    [RequiresDynamicCode("Uses reflection to find custom conversion operators and create arrays, which is not compatible with AOT compilation.")]
    [UnconditionalSuppressMessage("Trimming", "IL2072:Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute'")]
    private static bool TryReflectionConversion(Type targetType, Type sourceType, object value, out object? result)
    {
        // Ensure we're not in AOT mode
        ThrowIfAot(sourceType, targetType);

        // Handle array conversions
        if (targetType.IsArray && TryConvertArray(targetType, sourceType, value, out result))
        {
            return true;
        }

        // Try custom conversion operators (op_Implicit, op_Explicit)
        var conversionMethod = GetConversionMethodCached(sourceType, targetType);
        if (conversionMethod != null)
        {
            try
            {
                result = conversionMethod.Invoke(null, [value]);
                return true;
            }
            catch (Exception ex) when (ex is NotSupportedException || ex is InvalidOperationException)
            {
                // Reflection invoke failed - likely in AOT scenario despite our check
                // Try direct cast as fallback
                try
                {
                    result = value;
                    return true;
                }
                catch
                {
                    // Give up
                }
            }
        }

        // Try IConvertible for non-primitives
        if (value is IConvertible)
        {
            try
            {
                result = Convert.ChangeType(value, targetType);
                return true;
            }
            catch
            {
                // Conversion failed
            }
        }

        // Check for unboxing scenario
        if (targetType.IsValueType && sourceType == typeof(object))
        {
            try
            {
                result = value;
                return true;
            }
            catch
            {
                // Unboxing failed
            }
        }

        // Diagnostic logging for debugging
        if (Environment.GetEnvironmentVariable("TUNIT_DIAGNOSTIC_CAST") == "true")
        {
            Console.WriteLine($"[CastHelper] No conversion found from {sourceType.FullName} to {targetType.FullName}");
        }

        result = null;
        return false;
    }

    [RequiresDynamicCode("Array element conversion requires dynamic code for type inspection and conversion.")]
    [UnconditionalSuppressMessage("Trimming", "IL2072:Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute'")]
    private static bool TryConvertArray(Type targetType, Type sourceType, object value, out object? result)
    {
        var targetElementType = targetType.GetElementType()!;

        // Handle null -> empty array
        if (value is null)
        {
            result = Array.CreateInstance(targetElementType, 0);
            return true;
        }

        // Handle single value -> single element array
        if (!sourceType.IsArray)
        {
            if (value is IConvertible)
            {
                try
                {
                    var convertedValue = Convert.ChangeType(value, targetElementType);
                    var array = Array.CreateInstance(targetElementType, 1);
                    array.SetValue(convertedValue, 0);
                    result = array;
                    return true;
                }
                catch
                {
                    // Conversion failed
                }
            }

            result = null;
            return false;
        }

        // Handle array -> array with element type conversion
        if (value is Array sourceArray)
        {
            var sourceElementType = sourceType.GetElementType()!;

            // If element types match, return as-is
            if (sourceElementType == targetElementType)
            {
                result = value;
                return true;
            }

            // Convert each element
            try
            {
                var targetArray = Array.CreateInstance(targetElementType, sourceArray.Length);
                for (var i = 0; i < sourceArray.Length; i++)
                {
                    var sourceElement = sourceArray.GetValue(i);
                    var convertedElement = sourceElement is IConvertible
                        ? Convert.ChangeType(sourceElement, targetElementType)
                        : sourceElement;
                    targetArray.SetValue(convertedElement, i);
                }
                result = targetArray;
                return true;
            }
            catch
            {
                // Array conversion failed
            }
        }

        result = null;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.")]
    private static MethodInfo? GetConversionMethodCached(Type sourceType, Type targetType)
    {
        return ConversionMethodCache.GetOrAdd(
            (sourceType, targetType),
            static key => FindConversionMethod(key.Item1, key.Item2));
    }

    [RequiresDynamicCode("Finding conversion operators requires reflection which is not compatible with AOT compilation.")]
    [UnconditionalSuppressMessage("Trimming", "IL2070:Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute'")]
    private static MethodInfo? FindConversionMethod(Type sourceType, Type targetType)
    {
        // Get all static methods from both types
        var sourceMethods = sourceType.GetMethods(BindingFlags.Public | BindingFlags.Static);
        var targetMethods = targetType.GetMethods(BindingFlags.Public | BindingFlags.Static);

        // Look for implicit conversion first
        foreach (var method in sourceMethods)
        {
            if (method.Name == "op_Implicit" && method.ReturnType == targetType && HasCorrectInputType(sourceType, method))
            {
                return method;
            }
        }

        foreach (var method in targetMethods)
        {
            if (method.Name == "op_Implicit" && method.ReturnType == targetType && HasCorrectInputType(sourceType, method))
            {
                return method;
            }
        }

        // Look for explicit conversion
        foreach (var method in sourceMethods)
        {
            if (method.Name == "op_Explicit" && method.ReturnType == targetType && HasCorrectInputType(sourceType, method))
            {
                return method;
            }
        }

        foreach (var method in targetMethods)
        {
            if (method.Name == "op_Explicit" && method.ReturnType == targetType && HasCorrectInputType(sourceType, method))
            {
                return method;
            }
        }

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool HasCorrectInputType(Type expectedType, MethodInfo method)
    {
        var parameters = method.GetParameters();
        return parameters.Length == 1 && parameters[0].ParameterType == expectedType;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ThrowIfAot(Type sourceType, Type targetType)
    {
#if NET
        if (!RuntimeFeature.IsDynamicCodeSupported)
        {
            throw new NotSupportedException(
                $"Cannot convert {sourceType.Name} to {targetType.Name} in Native AOT mode. " +
                $"Reflection-based type conversion is not supported in AOT compilation. " +
                $"Consider registering a custom converter using AotConverterRegistry.Register() " +
                $"or use types that support direct casting.");
        }
#endif
    }
}
