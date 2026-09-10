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
        // Optimize: Use for-loop instead of LINQ to reduce allocations
        var paramTypes = new Type[method.Parameters.Length];
        for (int i = 0; i < method.Parameters.Length; i++)
        {
            paramTypes[i] = method.Parameters[i].Type;
        }
        return GetMethodFromType(method.Type, method.Name, paramTypes)!;
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
        // Optimize: Avoid LINQ Select in hot path - use manual parameter comparison
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy);

        foreach (var method in methods)
        {
            if (method.Name != name)
                continue;

            var methodParams = method.GetParameters();
            if (methodParams.Length != parameters.Length)
                continue;

            bool parametersMatch = true;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (methodParams[i].ParameterType != parameters[i])
                {
                    parametersMatch = false;
                    break;
                }
            }

            if (parametersMatch)
                return method;
        }

        return type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            ?? throw new InvalidOperationException($"Method '{name}' with parameters {string.Join(", ", parameters.Select(p => p.Name))} not found in type '{type.FullName}'.");
    }
}
