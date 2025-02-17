using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TUnit.Core.Helpers;

public class MethodInfoRetriever
{
    public static MethodInfo GetMethodInfo([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type, 
        string methodName, 
        int genericParameterCount, 
        Type[] parameterTypes)
    {
        if (genericParameterCount == 0)
        {
            return type.GetMethod(methodName, parameterTypes)
                   ?? throw new ArgumentException(
                       $"Method not found: {type}.{methodName}({string.Join(", ", parameterTypes.Select(x => x.Name))})");
        }

        return type
                   .GetMethods()
                   .Where(x => x.Name == methodName)
                   .Where(x => x.IsGenericMethod)
                   .Where(x => x.GetGenericArguments().Length == genericParameterCount)
                   .FirstOrDefault(x => x.GetParameters().Length == parameterTypes.Length)
               ?? throw new ArgumentException($"Method not found: {type}.{methodName}({string.Join(", ", parameterTypes.Select(x => x.Name))})");

    }
}