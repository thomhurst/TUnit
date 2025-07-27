using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TUnit.Core.Extensions;

public static class MetadataExtensions
{
    public static Type DeclaringType(this MethodMetadata method) => method.Class.Type;

    public static string MethodName(this MethodMetadata method) => method.Name;

    public static string DisplayName(this MethodMetadata method) => method.Name;

    public static bool IsGenericMethodDefinition(this MethodMetadata method) => method.GenericTypeCount > 0;

    public static MethodInfo GetReflectionInfo(this MethodMetadata method)
    {
        return GetMethodFromType(method.Type, method.Name, method.Parameters.Select(x => x.Type).ToArray())!;
    }

    public static IEnumerable<Attribute> GetCustomAttributes(this MethodMetadata method)
    {
        return
        [
            ..method.GetReflectionInfo().GetCustomAttributesSafe(),
            ..method.Type.GetCustomAttributesSafe(),
            ..method.Type.Assembly.GetCustomAttributesSafe()
        ];
    }

    /// <summary>
    /// Gets a method from the specified type with proper AOT attribution
    /// </summary>
    private static MethodInfo? GetMethodFromType(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type type,
        string name,
        Type[] parameters)
    {
        return type
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .SingleOrDefault(x => x.Name == name)
            ?? type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy, null, parameters, null)
            ?? throw new InvalidOperationException($"Method '{name}' with parameters {string.Join(", ", parameters.Select(p => p.Name))} not found in type '{type.FullName}'.");
    }
}
