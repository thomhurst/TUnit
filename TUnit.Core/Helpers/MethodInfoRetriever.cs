using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TUnit.Core.Helpers;

public class MethodInfoRetriever
{
    private static readonly BindingFlags BindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy;

    public static MethodInfo GetMethodInfo([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type type,
        string methodName,
        int genericParameterCount,
        Type[] parameterTypes)
    {
        if (genericParameterCount == 0)
        {
            return type.GetMethod(methodName, BindingFlags, null, parameterTypes, [])
                   ?? throw new ArgumentException(
                       $"Method not found: {type}.{methodName}({string.Join(", ", parameterTypes.Select(x => x.Name))})");
        }

        return type
                   .GetMethods(BindingFlags)
                   .Where(x => x.Name == methodName)
                   .Where(x => x.IsGenericMethod)
                   .Where(x => x.GetGenericArguments().Length == genericParameterCount)
                   .FirstOrDefault(x => x.GetParameters().Length == parameterTypes.Length)
               ?? throw new ArgumentException($"Method not found: {type}.{methodName}({string.Join(", ", parameterTypes.Select(x => x.Name))})");

    }
}
