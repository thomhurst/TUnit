using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TUnit.Core.Helpers;

public class MethodInfoRetriever
{
    public static MethodInfo? GetMethodInfo([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type, 
        string methodName, 
        int genericParameterCount, 
        Type[] parameterTypes)
    {
#if NET
            
        return type.GetMethod(methodName, genericParameterCount, parameterTypes);
#else
        if (genericParameterCount == 0)
        {
            return type.GetMethod(methodName, parameterTypes);
        }

        return type
            .GetMethods()
            .Where(x => x.Name == methodName)
            .Where(x => x.IsGenericMethod)
            .Where(x => x.GetGenericArguments().Length == genericParameterCount)
            .FirstOrDefault(x => x.GetParameters().Select(p => p.ParameterType).SequenceEqual(parameterTypes));
#endif
    }
}