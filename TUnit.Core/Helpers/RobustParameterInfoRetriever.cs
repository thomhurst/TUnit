using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TUnit.Core.Helpers;

/// <summary>
/// Provides robust parameter info retrieval for source-generated code with comprehensive fallback strategies.
/// </summary>
[RequiresUnreferencedCode("Uses reflection to retrieve parameter information")]
[RequiresDynamicCode("Uses reflection to retrieve parameter information")]
[Obsolete("This class uses reflection and is not compatible with AOT. Use source generators instead.")]
public static class RobustParameterInfoRetriever
{
    private static readonly BindingFlags RobustBindingFlags =
        BindingFlags.Instance
        | BindingFlags.Public
        | BindingFlags.NonPublic
        | BindingFlags.Static
        | BindingFlags.FlattenHierarchy;

    /// <summary>
    /// Retrieves constructor parameter info with comprehensive fallback strategies.
    /// </summary>
    /// <param name="type">The type containing the constructor</param>
    /// <param name="parameterIndex">The zero-based index of the parameter</param>
    /// <param name="parameterTypes">The expected parameter types for exact matching</param>
    /// <param name="expectedType"></param>
    /// <param name="parameterName"></param>
    /// <returns>The ParameterInfo for the specified parameter</returns>
    /// <exception cref="InvalidOperationException">Thrown when no suitable constructor is found</exception>
    public static ParameterInfo GetConstructorParameterInfo(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] Type type,
        Type[] parameterTypes,
        int parameterIndex,
        Type expectedType,
        string parameterName)
    {
        var parameterInfo = type.GetConstructor(RobustBindingFlags, null, parameterTypes, null)?.GetParameters()[parameterIndex];

        if (parameterInfo?.Name == parameterName && (parameterInfo.ParameterType == expectedType || parameterInfo.ParameterType.IsGenericParameter))
        {
            return parameterInfo;
        }

        foreach (var constructorInfo in type.GetConstructors(RobustBindingFlags).Where(x => x.GetParameters().Length == parameterTypes.Length))
        {
            foreach (var parameter in constructorInfo.GetParameters())
            {
                if (parameter.Name == parameterName && (parameter.ParameterType == expectedType || parameter.ParameterType.IsGenericParameter))
                {
                    return parameter;
                }
            }
        }

        foreach (var constructorInfo in type.GetConstructors(RobustBindingFlags))
        {
            foreach (var parameter in constructorInfo.GetParameters())
            {
                if (parameter.Name == parameterName && (parameter.ParameterType == expectedType || parameter.ParameterType.IsGenericParameter))
                {
                    return parameter;
                }
            }
        }

        return null!;
    }

    /// <summary>
    /// Retrieves method parameter info with comprehensive fallback strategies.
    /// </summary>
    /// <param name="type">The type containing the method</param>
    /// <param name="methodName">The name of the method</param>
    /// <param name="parameterIndex">The zero-based index of the parameter</param>
    /// <param name="parameterTypes">The expected parameter types for exact matching</param>
    /// <param name="isStatic">Whether the method is static</param>
    /// <param name="genericParameterCount">The number of generic parameters the method has</param>
    /// <returns>The ParameterInfo for the specified parameter</returns>
    /// <exception cref="InvalidOperationException">Thrown when no suitable method is found</exception>
    public static ParameterInfo GetMethodParameterInfo(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicProperties)] Type type,
        string methodName,
        int parameterIndex,
        Type[] parameterTypes,
        bool isStatic,
        int genericParameterCount)
    {
        try
        {
            var bindingFlags = RobustBindingFlags;

            if (isStatic)
            {
                bindingFlags &= ~BindingFlags.Instance;
            }
            else
            {
                bindingFlags &= ~BindingFlags.Static;
            }

            if (genericParameterCount == 0)
            {
                var method = type.GetMethod(methodName, bindingFlags, null, parameterTypes, null);

                if (method != null)
                {
                    return method.GetParameters()[parameterIndex];
                }
            }

            var methods = type.GetMethods(bindingFlags)
                .Where(m => m.Name == methodName)
                .Where(m => m.GetGenericArguments().Length == genericParameterCount)
                .Where(m => m.GetParameters().Length == parameterTypes.Length)
                .ToArray();

            if (methods.Length > 0)
            {
                // Prefer exact type match if available
                var exactMatch = methods.FirstOrDefault(m => ParameterTypesMatch(m.GetParameters(), parameterTypes));

                if (exactMatch != null)
                {
                    return exactMatch.GetParameters()[parameterIndex];
                }

                return methods[0].GetParameters()[parameterIndex];
            }

            var methodByCount = type
                .GetMethods(bindingFlags)
                .Where(m => m.Name == methodName)
                .FirstOrDefault(m => m.GetParameters().Length == parameterTypes.Length);

            if (methodByCount != null)
            {
                return methodByCount.GetParameters()[parameterIndex];
            }

            var methodByName = type.GetMethods(bindingFlags)
                .Where(m => m.Name == methodName)
                .Where(m => m.GetParameters().Length > parameterIndex)
                .OrderBy(m => m.GetParameters().Length)
                .FirstOrDefault();

            if (methodByName != null)
            {
                return methodByName.GetParameters()[parameterIndex];
            }

            var methodAny = type
                .GetMethods(RobustBindingFlags)
                .Where(m => m.Name == methodName)
                .FirstOrDefault(m => m.GetParameters().Length > parameterIndex);

            if (methodAny != null)
            {
                return methodAny.GetParameters()[parameterIndex];
            }

            throw new InvalidOperationException($"No suitable method found for type {type.FullName}, method {methodName} with parameter index {parameterIndex}");
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException($"Failed to get method parameter info for type {type.FullName}, method {methodName}, parameter index {parameterIndex}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Checks if parameter types match, accounting for generic parameters, nullable types, and assignability.
    /// </summary>
    private static bool ParameterTypesMatch(ParameterInfo[] parameters, Type[] expectedTypes)
    {
        if (parameters.Length != expectedTypes.Length)
        {
            return false;
        }

        for (int i = 0; i < parameters.Length; i++)
        {
            var paramType = parameters[i].ParameterType;
            var expectedType = expectedTypes[i];

            // Exact match
            if (paramType == expectedType)
            {
                continue;
            }

            // Handle generic parameters and open generic types
            if (paramType.IsGenericParameter || expectedType.IsGenericParameter)
            {
                continue;
            }

            // Handle nullable types
            var underlyingParamType = Nullable.GetUnderlyingType(paramType) ?? paramType;
            var underlyingExpectedType = Nullable.GetUnderlyingType(expectedType) ?? expectedType;

            if (underlyingParamType == underlyingExpectedType)
            {
                continue;
            }

            // Check assignability
            if (expectedType.IsAssignableFrom(paramType) || paramType.IsAssignableFrom(expectedType))
            {
                continue;
            }

            return false;
        }

        return true;
    }
}
