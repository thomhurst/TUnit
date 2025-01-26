using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TUnit.Core.Helpers;

public static class CastHelper
{
    [UnconditionalSuppressMessage("", "IL2072")]
    public static T? Cast<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(object? value)
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

        return (T?) GetImplicitConversion(value.GetType(), typeof(T)).Invoke(null, [value]);
    }

    private static MethodInfo GetImplicitConversion([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type baseType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type targetType)
    {
        return baseType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Concat(targetType.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .Where(mi => mi.Name == "op_Implicit" && mi.ReturnType == targetType)
            .FirstOrDefault(mi =>
            {
                var pi = mi.GetParameters().FirstOrDefault();
                return pi != null && pi.ParameterType == baseType;
            }) ?? throw new ArgumentException($"Cannot convert from {baseType} to {targetType}");
    }
}