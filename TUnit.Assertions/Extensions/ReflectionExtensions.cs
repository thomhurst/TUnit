using System.Diagnostics.CodeAnalysis;

namespace TUnit.Assertions.Extensions;

internal static class ReflectionExtensions
{
    [RequiresUnreferencedCode("Reflection")]
    public static object? GetPropertyValue(this object obj, string propertyName)
    {
        return obj.GetType().GetProperty(propertyName)?.GetValue(obj);
    }
    
    [RequiresUnreferencedCode("Reflection")]
    public static object? GetMethodReturnValue(this object obj, string methodName)
    {
        return obj.GetType().GetMethod(methodName)?.Invoke(obj, null);
    }
}