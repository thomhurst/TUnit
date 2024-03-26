namespace TUnit.Assertions.Extensions;

public static class ReflectionExtensions
{
    internal static object? GetPropertyValue(this object obj, string propertyName)
    {
        return obj.GetType().GetProperty(propertyName)?.GetValue(obj);
    }
    
    internal static object? GetMethodReturnValue(this object obj, string methodName)
    {
        return obj.GetType().GetMethod(methodName)?.Invoke(obj, null);
    }
}