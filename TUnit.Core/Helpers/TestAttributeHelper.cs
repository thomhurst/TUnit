using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TUnit.Core.Helpers;

public static class TestAttributeHelper
{
    /// <summary>
    /// Converts an array of attributes to TestAttributeMetadata instances with metadata
    /// </summary>
    public static AttributeMetadata[] ConvertToTestAttributes(
        Attribute[] attributes, 
        TestAttributeTarget targetElement,
        string? targetMemberName = null,
        Type? targetType = null,
        ClassMetadata? classMetadata = null)
    {
        return attributes.Select(attr => new AttributeMetadata
        {
            Instance = attr,
            TargetElement = targetElement,
            TargetMemberName = targetMemberName,
            TargetType = targetType,
            ClassMetadata = classMetadata,
            ConstructorArguments = GetConstructorArguments(attr),
            NamedArguments = GetNamedArguments(attr)
        }).ToArray();
    }

    /// <summary>
    /// Converts assembly attributes to TestAttributeMetadata instances
    /// </summary>
    public static AttributeMetadata[] FromAssembly(Assembly assembly)
    {
        var attributes = assembly.GetCustomAttributes().ToArray();
        return ConvertToTestAttributes(attributes, TestAttributeTarget.Assembly, assembly.GetName().Name);
    }

    /// <summary>
    /// Converts type attributes to TestAttributeMetadata instances
    /// </summary>
    public static AttributeMetadata[] FromType(Type type)
    {
        var attributes = type.GetCustomAttributes().ToArray();
        return ConvertToTestAttributes(attributes, TestAttributeTarget.Class, type.Name, type);
    }

    /// <summary>
    /// Converts method attributes to TestAttributeMetadata instances
    /// </summary>
    public static AttributeMetadata[] FromMethod(MethodInfo method)
    {
        var attributes = method.GetCustomAttributes().ToArray();
        return ConvertToTestAttributes(attributes, TestAttributeTarget.Method, method.Name, method.DeclaringType);
    }

    /// <summary>
    /// Converts property attributes to TestAttributeMetadata instances
    /// </summary>
    public static AttributeMetadata[] FromProperty(PropertyInfo property)
    {
        var attributes = property.GetCustomAttributes().ToArray();
        return ConvertToTestAttributes(attributes, TestAttributeTarget.Property, property.Name, property.DeclaringType);
    }

    /// <summary>
    /// Converts parameter attributes to TestAttributeMetadata instances
    /// </summary>
    public static AttributeMetadata[] FromParameter(ParameterInfo parameter)
    {
        var attributes = parameter.GetCustomAttributes().ToArray();
        return ConvertToTestAttributes(attributes, TestAttributeTarget.Parameter, parameter.Name, parameter.Member.DeclaringType);
    }

    private static object?[]? GetConstructorArguments(Attribute attribute)
    {
        // In reflection mode, we can't easily get the original constructor arguments
        // This would need to be populated during source generation
        return null;
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:GetProperties", Justification = "We're accessing properties on known attribute types")]
    private static IDictionary<string, object?>? GetNamedArguments(Attribute attribute)
    {
        var type = attribute.GetType();
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite);

        var namedArgs = new Dictionary<string, object?>();
        
        foreach (var prop in properties)
        {
            try
            {
                var value = prop.GetValue(attribute);
                #pragma warning disable IL2072 // Target parameter argument does not satisfy DynamicallyAccessedMembersAttribute
                var defaultValue = prop.PropertyType.IsValueType ? Activator.CreateInstance(prop.PropertyType) : null;
                #pragma warning restore IL2072
                
                // Only include if the value is different from the default
                if (!Equals(value, defaultValue))
                {
                    namedArgs[prop.Name] = value;
                }
            }
            catch
            {
                // Skip properties that throw on access
            }
        }

        return namedArgs.Count > 0 ? namedArgs : null;
    }
}