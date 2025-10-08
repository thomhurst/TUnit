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
    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.")]
    [UnconditionalSuppressMessage("Trimming", "IL2072:Target parameter argument does not satisfy \'DynamicallyAccessedMembersAttribute\' in call to target method. The return value of the source method does not have matching annotations.")]
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

        var sourceType = value.GetType();

        if (sourceType.IsAssignableTo(underlyingType))
        {
            return (T) value;
        }

        // Try AOT converter registry first
        if (AotConverterRegistry.TryConvert(sourceType, underlyingType, value, out var converted))
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
            && !sourceType.IsArray  // Don't unwrap arrays
            && !typeof(IEnumerable).IsAssignableFrom(typeof(T)))
        {
            value = enumerable.Cast<object>().ElementAtOrDefault(0);
        }

        if (underlyingType.IsEnum)
        {
            return (T?) Enum.ToObject(underlyingType, value!);
        }

        var conversionMethod = GetConversionMethod(value!.GetType(), underlyingType);

        if (conversionMethod is null && value is IConvertible)
        {
            return (T?) Convert.ChangeType(value, underlyingType);
        }

        if (conversionMethod is null)
        {
            // Check if we can do unboxing directly for value types
            if (underlyingType.IsValueType && sourceType == typeof(object))
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
                Console.WriteLine($"[CastHelper] No conversion found from {sourceType.FullName} to {underlyingType.FullName}");
            }

            return (T?) value;
        }

        // Even in source generation mode, we use reflection as a fallback for custom conversions
        // The AOT analyzer will warn about incompatibility at compile time
        try
        {
            return (T?) conversionMethod.Invoke(null, [value]);
        }
        catch (Exception ex) when (ex is NotSupportedException or InvalidOperationException)
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

    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.")]
    [UnconditionalSuppressMessage("Trimming", "IL2072:Target parameter argument does not satisfy \'DynamicallyAccessedMembersAttribute\' in call to target method. The return value of the source method does not have matching annotations.")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object? Cast([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type, object? value)
    {
        if (value is null)
        {
            return null;
        }

        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        var sourceType = value.GetType();

        if (sourceType.IsAssignableTo(underlyingType))
        {
            return value;
        }

        // Try AOT converter registry first
        if (AotConverterRegistry.TryConvert(sourceType, underlyingType, value, out var converted))
        {
            return converted;
        }

        if (type.IsGenericParameter)
        {
            return value;
        }

        if (value is not string
            && value is IEnumerable enumerable
            && !sourceType.IsArray  // Don't unwrap arrays
            && !typeof(IEnumerable).IsAssignableFrom(type))
        {
            value = enumerable.Cast<object>().ElementAtOrDefault(0);
        }

        if (underlyingType.IsEnum)
        {
            return Enum.ToObject(underlyingType, value!);
        }

        var conversionMethod = GetConversionMethod(value!.GetType(), underlyingType);

        if (conversionMethod is null && value is IConvertible)
        {
            return Convert.ChangeType(value, underlyingType);
        }

        if (conversionMethod is null)
        {
            // Check if we can do unboxing directly for value types
            if (underlyingType.IsValueType && sourceType == typeof(object))
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

    private static MethodInfo? GetConversionMethod([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type baseType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type targetType)
    {
        // In single file mode, we might need to look harder for conversion methods
        // First try the base type methods (including inherited and declared only)
        var baseMethods = baseType.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Concat(baseType.GetMethods(BindingFlags.Public | BindingFlags.Static));

        // Then try the target type methods
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

    private static bool HasCorrectInputType(Type baseType, MethodInfo mi)
    {
        var pi = mi.GetParameters().FirstOrDefault();
        return pi != null && pi.ParameterType == baseType;
    }
}
