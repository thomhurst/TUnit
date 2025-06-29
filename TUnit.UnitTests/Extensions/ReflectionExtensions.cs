using System.Reflection;

namespace TUnit.UnitTests.Extensions;

public static class ReflectionExtensions
{
    public static FieldInfo? GetBackingField(this PropertyInfo propertyInfo)
    {
        return propertyInfo.DeclaringType!.GetField($"<{propertyInfo.Name}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
    }
}
