﻿using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TUnit.Core.Helpers;

public static class CastHelper
{
    [UnconditionalSuppressMessage("", "IL2072")]
    public static T? Cast<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>(object? value)
    {
        if (value is null)
        {
            return default;
        }

        if (value.GetType().IsAssignableTo<T>())
        {
            return (T)value;
        }
        
        if (typeof(T).IsEnum)
        {
            return (T?) Enum.ToObject(typeof(T), value);
        }

        var conversionMethod = GetConversionMethod(value.GetType(), typeof(T));

        if (conversionMethod is null)
        {
            return (T?) Convert.ChangeType(value, typeof(T));
        }
        
        return (T?) conversionMethod.Invoke(null, [value]);
    }

    private static MethodInfo? GetConversionMethod([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type baseType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type targetType)
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