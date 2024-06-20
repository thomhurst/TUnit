using System.Reflection;

namespace TUnit.Engine.Helpers;

public static class AttributeHelper
{
    public static TAttribute? GetAttribute<TAttribute>(Type type, MethodInfo methodInfo) where TAttribute : Attribute
    {
        return methodInfo.GetCustomAttributes<TAttribute>()
            .Concat(type.GetCustomAttributes<TAttribute>())
            .FirstOrDefault();
    }
}