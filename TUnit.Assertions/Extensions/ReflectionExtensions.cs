using System.Diagnostics.CodeAnalysis;

namespace TUnit.Assertions.Extensions;

internal static class ReflectionExtensions
{
    [RequiresUnreferencedCode("Reflection")]
    public static object? GetPropertyValue(this object obj, string propertyName)
    {
        return GetPropertyFromType(obj.GetType(), propertyName)?.GetValue(obj);
    }

    [RequiresUnreferencedCode("Reflection")]
    public static object? GetMethodReturnValue(this object obj, string methodName)
    {
        return GetMethodFromType(obj.GetType(), methodName)?.Invoke(obj, null);
    }
    
    /// <summary>
    /// Gets a property from the specified type with proper AOT attribution
    /// </summary>
    [RequiresUnreferencedCode("Reflection")]
    private static System.Reflection.PropertyInfo? GetPropertyFromType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type, string propertyName)
    {
        return type.GetProperty(propertyName);
    }
    
    /// <summary>
    /// Gets a method from the specified type with proper AOT attribution
    /// </summary>
    [RequiresUnreferencedCode("Reflection")]
    private static System.Reflection.MethodInfo? GetMethodFromType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type, string methodName)
    {
        return type.GetMethod(methodName);
    }
}
