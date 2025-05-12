using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core.Extensions;

namespace TUnit.Core.Helpers;

internal class SourceModelHelpers
{
    public static SourceGeneratedMethodInformation BuildTestMethod([DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicConstructors
        | DynamicallyAccessedMemberTypes.PublicMethods
        | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type testClassType, MethodInfo methodInfo, Dictionary<string, object?> properties, string? testName)
    {
        return new SourceGeneratedMethodInformation
        {
            Attributes = methodInfo.GetCustomAttributes().ToArray(),
            Class = GenerateClass(testClassType, properties),
            Name = testName ?? methodInfo.Name,
            GenericTypeCount = methodInfo.IsGenericMethod ? methodInfo.GetGenericArguments().Length : 0,
            Parameters = GetParameters(methodInfo.GetParameters()),
            Type = testClassType,
            ReflectionInformation = methodInfo,
            ReturnType = methodInfo.ReturnType
        };
    }

    public static SourceGeneratedClassInformation GenerateClass([DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicConstructors
        | DynamicallyAccessedMemberTypes.PublicMethods
        | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type testClassType, Dictionary<string, object?>? properties)
    {
        return new SourceGeneratedClassInformation
        {
            Assembly = GenerateAssembly(testClassType),
            Attributes = testClassType.GetCustomAttributes().ToArray(),
            Name = testClassType.GetFormattedName(),
            Namespace = testClassType.Namespace,
            Parameters = GetParameters(testClassType.GetConstructors().FirstOrDefault()?.GetParameters() ?? []).ToArray(),
            Properties = properties?.Select(GenerateProperty).ToArray() ?? [],
            Type = testClassType
        };
    }

    public static SourceGeneratedAssemblyInformation GenerateAssembly(Type testClassType)
    {
        return new SourceGeneratedAssemblyInformation
        {
            Attributes = testClassType.Assembly.GetCustomAttributes().ToArray(),
            Name = testClassType.Assembly.GetName().Name ??
                   testClassType.Assembly.GetName().FullName,
        };
    }

    public static SourceGeneratedPropertyInformation GenerateProperty(KeyValuePair<string, object?> property)
    {
        return new SourceGeneratedPropertyInformation
        {
            Attributes = [], // TODO?
            Name = property.Key,
#pragma warning disable IL2072
            Type = property.Value?.GetType() ?? typeof(object),
#pragma warning restore IL2072
            IsStatic = false, // TODO?
        };
    }

    public static SourceGeneratedParameterInformation[] GetParameters(ParameterInfo[] parameters)
    {
        return parameters.Select(GenerateParameter).ToArray();
    }

    public static SourceGeneratedParameterInformation GenerateParameter(ParameterInfo parameter)
    {
        return new SourceGeneratedParameterInformation(parameter.ParameterType)
        {
            Attributes = parameter.GetCustomAttributes().ToArray(),
            Name = parameter.Name ?? string.Empty,
        };
    }
}