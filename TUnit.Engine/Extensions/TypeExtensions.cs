using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TUnit.Engine.Extensions;

public static class TypeExtensions
{
    public static bool HasAttribute<T>(this Type type, [NotNullWhen(true)] out T[]? attributes) where T : Attribute
    {
        attributes = type.GetCustomAttributes<T>().ToArray();
        return attributes.Any();
    }
}