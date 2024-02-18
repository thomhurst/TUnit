namespace TUnit.Assertions.Extensions;

public static class ReflectionExtensions
{
    public static object? GetPropertyValue(this object obj, string propertyName)
    {
        return obj.GetType().GetProperty(propertyName)?.GetValue(obj);
    }
    
    public static object? GetMethodReturnValue(this object obj, string methodName)
    {
        return obj.GetType().GetMethod(methodName)?.Invoke(obj, null);
    }
}