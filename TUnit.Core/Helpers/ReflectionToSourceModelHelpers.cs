using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core.Extensions;

namespace TUnit.Core.Helpers;

[RequiresDynamicCode("Reflection")]
[RequiresUnreferencedCode("Reflection")]
internal class ReflectionToSourceModelHelpers
{
    private static readonly ConcurrentDictionary<Assembly, SourceGeneratedAssemblyInformation> _assemblyCache = new();
    private static readonly ConcurrentDictionary<Type, SourceGeneratedClassInformation> _classCache = new();
    private static readonly ConcurrentDictionary<MethodInfo, SourceGeneratedMethodInformation> _methodCache = new();
    private static readonly ConcurrentDictionary<PropertyInfo, SourceGeneratedPropertyInformation> _propertyCache = new();
    private static readonly ConcurrentDictionary<ParameterInfo, SourceGeneratedParameterInformation> _parameterCache = new();

    public static SourceGeneratedMethodInformation BuildTestMethod([DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicConstructors
        | DynamicallyAccessedMemberTypes.PublicMethods
        | DynamicallyAccessedMemberTypes.NonPublicMethods
        | DynamicallyAccessedMemberTypes.PublicProperties)] Type testClassType, MethodInfo methodInfo, string? testName)
    {
        return BuildTestMethod(GenerateClass(testClassType), methodInfo, testName);
    }

    public static SourceGeneratedMethodInformation BuildTestMethod(SourceGeneratedClassInformation classInformation, MethodInfo methodInfo, string? testName)
    {
        return _methodCache.GetOrAdd(methodInfo, _ => new SourceGeneratedMethodInformation
        {
            Attributes = methodInfo.GetCustomAttributes().ToArray(),
            Class = classInformation,
            Name = testName ?? methodInfo.Name,
            GenericTypeCount = methodInfo.IsGenericMethod ? methodInfo.GetGenericArguments().Length : 0,
            Parameters = GetParameters(methodInfo.GetParameters()),
            Type = classInformation.Type,
            ReflectionInformation = methodInfo,
            ReturnType = methodInfo.ReturnType
        });
    }

    public static SourceGeneratedClassInformation? GetParent(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors
            | DynamicallyAccessedMemberTypes.PublicMethods
            | DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
    {
        if (type.DeclaringType is null)
        {
            return null;
        }

        return GenerateClass(type.DeclaringType);
    }

    public static SourceGeneratedClassInformation GenerateClass(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors
        | DynamicallyAccessedMemberTypes.PublicMethods
        | DynamicallyAccessedMemberTypes.NonPublicMethods
        | DynamicallyAccessedMemberTypes.PublicProperties)] Type testClassType)
    {
        return _classCache.GetOrAdd(testClassType, _ => new SourceGeneratedClassInformation
        {
            Parent = GetParent(testClassType),
            Assembly = GenerateAssembly(testClassType),
            Attributes = testClassType.GetCustomAttributes().ToArray(),
            Name = testClassType.GetFormattedName(),
            Namespace = testClassType.Namespace,
            Parameters = GetParameters(testClassType.GetConstructors().FirstOrDefault()?.GetParameters() ?? []).ToArray(),
            Properties = testClassType.GetProperties().Select(GenerateProperty).ToArray(),
            Type = testClassType
        });
    }

    public static SourceGeneratedAssemblyInformation GenerateAssembly(Type testClassType)
    {
        return _assemblyCache.GetOrAdd(testClassType.Assembly, _ => new SourceGeneratedAssemblyInformation
        {
            Attributes = testClassType.Assembly.GetCustomAttributes().ToArray(),
            Name = testClassType.Assembly.GetName().Name ??
                   testClassType.Assembly.GetName().FullName,
        });
    }

    public static SourceGeneratedPropertyInformation GenerateProperty(PropertyInfo property)
    {
        return _propertyCache.GetOrAdd(property, _ => new SourceGeneratedPropertyInformation
        {
            Attributes = property.GetCustomAttributes().ToArray(),
            ReflectionInfo = property,
            Name = property.Name,
            Type = property.PropertyType,
            IsStatic = property.GetMethod?.IsStatic is true
                || property.SetMethod?.IsStatic is true,
            Getter = property.GetValue
        });
    }

    public static SourceGeneratedParameterInformation[] GetParameters(ParameterInfo[] parameters)
    {
        return parameters.Select(GenerateParameter).ToArray();
    }

    public static SourceGeneratedParameterInformation GenerateParameter(ParameterInfo parameter)
    {
        return _parameterCache.GetOrAdd(parameter, _ => new SourceGeneratedParameterInformation(parameter.ParameterType)
        {
            Attributes = parameter.GetCustomAttributes().ToArray(),
            Name = parameter.Name ?? string.Empty,
            ReflectionInfo = parameter,
        });
    }
}
