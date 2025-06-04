using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core.Extensions;

namespace TUnit.Core.Helpers;

[RequiresDynamicCode("Reflection")]
[RequiresUnreferencedCode("Reflection")]
internal class ReflectionToSourceModelHelpers
{
    public static SourceGeneratedMethodInformation BuildTestMethod([DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicConstructors
        | DynamicallyAccessedMemberTypes.PublicMethods
        | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type testClassType, MethodInfo methodInfo, string? testName)
    {
        return new SourceGeneratedMethodInformation
        {
            Attributes = methodInfo.GetCustomAttributes().ToArray(),
            Class = GenerateClass(testClassType),
            Name = testName ?? methodInfo.Name,
            GenericTypeCount = methodInfo.IsGenericMethod ? methodInfo.GetGenericArguments().Length : 0,
            Parameters = GetParameters(methodInfo.GetParameters()),
            Type = testClassType,
            ReflectionInformation = methodInfo,
            ReturnType = methodInfo.ReturnType
        };
    }

    public static SourceGeneratedClassInformation? GetParent(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors
            | DynamicallyAccessedMemberTypes.PublicMethods
            | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type type)
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
        | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type testClassType)
    {
        return new SourceGeneratedClassInformation
        {
            Parent = GetParent(testClassType),
            Assembly = GenerateAssembly(testClassType),
            Attributes = testClassType.GetCustomAttributes().ToArray(),
            Name = testClassType.GetFormattedName(),
            Namespace = testClassType.Namespace,
            Parameters = GetParameters(testClassType.GetConstructors().FirstOrDefault()?.GetParameters() ?? []).ToArray(),
            Properties = testClassType.GetProperties().Select(GenerateProperty).ToArray(),
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

    public static SourceGeneratedPropertyInformation GenerateProperty(PropertyInfo property)
    {
        return new SourceGeneratedPropertyInformation
        {
            Attributes = property.GetCustomAttributes().ToArray(),
            Name = property.Name,
            Type = property.PropertyType,
            IsStatic = property.GetMethod?.IsStatic is true
                || property.SetMethod?.IsStatic is true,
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
            IsOptional = parameter.IsOptional,
            DefaultValue = parameter.HasDefaultValue ? parameter.DefaultValue : null,
        };
    }
}
