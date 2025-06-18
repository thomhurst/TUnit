using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core.Extensions;

/// <summary>
/// Extension methods for working with TestAttributeMetadata instances
/// </summary>
public static class TestAttributeExtensions
{
    /// <summary>
    /// Gets all TestAttributeMetadata instances of a specific type
    /// </summary>
    public static IEnumerable<AttributeMetadata> OfAttributeType<TAttribute>(this IEnumerable<AttributeMetadata> attributes) 
        where TAttribute : Attribute
    {
        return attributes.Where(ta => ta.Instance is TAttribute);
    }

    /// <summary>
    /// Gets all attribute instances of a specific type
    /// </summary>
    public static IEnumerable<TAttribute> OfType<TAttribute>(this IEnumerable<AttributeMetadata> attributes)
        where TAttribute : Attribute
    {
        return attributes.Where(ta => ta.Instance is TAttribute).Select(ta => (TAttribute)ta.Instance);
    }

    /// <summary>
    /// Gets the first TestAttributeMetadata instance of a specific type, or null if not found
    /// </summary>
    public static AttributeMetadata? FirstOfAttributeType<TAttribute>(this IEnumerable<AttributeMetadata> attributes) 
        where TAttribute : Attribute
    {
        return attributes.FirstOrDefault(ta => ta.Instance is TAttribute);
    }

    /// <summary>
    /// Gets the attribute instance as a specific type
    /// </summary>
    public static TAttribute? GetInstance<TAttribute>(this AttributeMetadata attribute) 
        where TAttribute : Attribute
    {
        return attribute.Instance as TAttribute;
    }

    /// <summary>
    /// Checks if a TestAttributeMetadata has a specific attribute type
    /// </summary>
    public static bool IsAttributeType<TAttribute>(this AttributeMetadata attribute) 
        where TAttribute : Attribute
    {
        return attribute.Instance is TAttribute;
    }

    /// <summary>
    /// Gets all TestAttributeMetadata instances applied to a specific target type
    /// </summary>
    public static IEnumerable<AttributeMetadata> ForTarget(this IEnumerable<AttributeMetadata> attributes, TestAttributeTarget target)
    {
        return attributes.Where(ta => ta.TargetElement == target);
    }

    /// <summary>
    /// Gets all TestAttributeMetadata instances applied to a specific member
    /// </summary>
    public static IEnumerable<AttributeMetadata> ForMember(this IEnumerable<AttributeMetadata> attributes, string memberName)
    {
        return attributes.Where(ta => ta.TargetMemberName == memberName);
    }

    /// <summary>
    /// Gets the value of a constructor argument by index
    /// </summary>
    public static object? GetConstructorArgument(this AttributeMetadata attribute, int index)
    {
        if (attribute.ConstructorArguments == null || index >= attribute.ConstructorArguments.Length)
        {
            return null;
        }
        return attribute.ConstructorArguments[index];
    }

    /// <summary>
    /// Gets the value of a named argument (property)
    /// </summary>
    public static object? GetNamedArgument(this AttributeMetadata attribute, string propertyName)
    {
        if (attribute.NamedArguments == null || !attribute.NamedArguments.TryGetValue(propertyName, out var value))
        {
            return null;
        }
        return value;
    }

    /// <summary>
    /// Tries to get the value of a named argument (property)
    /// </summary>
    public static bool TryGetNamedArgument(this AttributeMetadata attribute, string propertyName, [NotNullWhen(true)] out object? value)
    {
        value = null;
        if (attribute.NamedArguments == null)
        {
            return false;
        }
        return attribute.NamedArguments.TryGetValue(propertyName, out value);
    }
}