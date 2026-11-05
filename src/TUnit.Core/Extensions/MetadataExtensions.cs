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
        return method.ReflectionInfo ?? GetMethodFromType(method.Type, method.Name, method.Parameters, method.GenericTypeCount);
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
    private static MethodInfo GetMethodFromType(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type type,
        string name,
        ParameterMetadata[] parameters,
        int genericTypeCount)
    {
        // Optimize: Avoid LINQ Select in hot path - use manual parameter comparison
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy);

        MethodInfo? soleCandidate = null;
        var candidateCount = 0;
        foreach (var method in methods)
        {
            if (method.Name != name || (method.IsGenericMethod ? method.GetGenericArguments().Length : 0) != genericTypeCount)
                continue;

            var methodParams = method.GetParameters();
            if (methodParams.Length != parameters.Length)
                continue;

            soleCandidate = method;
            candidateCount++;
            bool parametersMatch = true;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (!(method.ContainsGenericParameters
                    ? MatchesTypeInfo(methodParams[i].ParameterType, parameters[i].TypeInfo)
                    : methodParams[i].ParameterType == parameters[i].Type))
                {
                    parametersMatch = false;
                    break;
                }
            }

            if (parametersMatch)
                return method;
        }

        // Older metadata can erase shapes such as T[] to object. Preserve its unambiguous
        // lookup without allowing a name-only fallback to select the wrong overload.
        if (candidateCount == 1)
        {
            return soleCandidate!;
        }

        throw new InvalidOperationException($"Method '{name}' with {genericTypeCount} generic parameter(s) and {parameters.Length} parameter(s) not found in type '{type.FullName}'.");
    }

    private static bool MatchesTypeInfo(Type type, TypeInfo typeInfo)
    {
        switch (typeInfo)
        {
            case ConcreteType concrete:
                return type == concrete.Type;
            case GenericParameter generic:
                return type.IsGenericParameter && type.GenericParameterPosition == generic.Position
                    && (type.DeclaringMethod is not null) == generic.IsMethodParameter;
            case ConstructedGeneric constructed:
                if (!type.IsGenericType || type.GetGenericTypeDefinition() != constructed.GenericDefinition)
                {
                    return false;
                }
                var arguments = type.GetGenericArguments();
                if (arguments.Length != constructed.TypeArguments.Length)
                {
                    return false;
                }
                for (var i = 0; i < arguments.Length; i++)
                {
                    if (!MatchesTypeInfo(arguments[i], constructed.TypeArguments[i]))
                    {
                        return false;
                    }
                }
                return true;
            default:
                return false;
        }
    }
}
