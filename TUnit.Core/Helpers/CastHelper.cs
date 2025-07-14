using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TUnit.Core.Helpers;

public static class CastHelper
{
    [UnconditionalSuppressMessage("", "IL2072")]
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

        if (value is not string
            && value is IEnumerable enumerable
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
            return (T?) value;
        }

        return (T?) conversionMethod.Invoke(null, [value]);
    }

    [UnconditionalSuppressMessage("", "IL2072")]
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

        if (type.IsGenericParameter)
        {
            return value;
        }

        if (value is not string
            && value is IEnumerable enumerable
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
            return value;
        }

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
}
