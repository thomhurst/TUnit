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
    public static IEnumerable<TestAttributeMetadata> OfAttributeType<TAttribute>(this IEnumerable<TestAttributeMetadata> attributes) 
        where TAttribute : Attribute
    {
        return attributes.Where(ta => ta.Instance is TAttribute);
    }

    /// <summary>
    /// Gets all attribute instances of a specific type
    /// </summary>
    public static IEnumerable<TAttribute> OfType<TAttribute>(this IEnumerable<TestAttributeMetadata> attributes)
        where TAttribute : Attribute
    {
        return attributes.Where(ta => ta.Instance is TAttribute).Select(ta => (TAttribute)ta.Instance);
    }

    /// <summary>
    /// Gets the first TestAttributeMetadata instance of a specific type, or null if not found
    /// </summary>
    public static TestAttributeMetadata? FirstOfAttributeType<TAttribute>(this IEnumerable<TestAttributeMetadata> attributes) 
        where TAttribute : Attribute
    {
        return attributes.FirstOrDefault(ta => ta.Instance is TAttribute);
    }

    /// <summary>
    /// Gets the attribute instance as a specific type
    /// </summary>
    public static TAttribute? GetInstance<TAttribute>(this TestAttributeMetadata testAttribute) 
        where TAttribute : Attribute
    {
        return testAttribute.Instance as TAttribute;
    }

    /// <summary>
    /// Checks if a TestAttributeMetadata has a specific attribute type
    /// </summary>
    public static bool IsAttributeType<TAttribute>(this TestAttributeMetadata testAttribute) 
        where TAttribute : Attribute
    {
        return testAttribute.Instance is TAttribute;
    }

    /// <summary>
    /// Gets all TestAttributeMetadata instances applied to a specific target type
    /// </summary>
    public static IEnumerable<TestAttributeMetadata> ForTarget(this IEnumerable<TestAttributeMetadata> attributes, TestAttributeTarget target)
    {
        return attributes.Where(ta => ta.TargetElement == target);
    }

    /// <summary>
    /// Gets all TestAttributeMetadata instances applied to a specific member
    /// </summary>
    public static IEnumerable<TestAttributeMetadata> ForMember(this IEnumerable<TestAttributeMetadata> attributes, string memberName)
    {
        return attributes.Where(ta => ta.TargetMemberName == memberName);
    }

    /// <summary>
    /// Gets the value of a constructor argument by index
    /// </summary>
    public static object? GetConstructorArgument(this TestAttributeMetadata testAttribute, int index)
    {
        if (testAttribute.ConstructorArguments == null || index >= testAttribute.ConstructorArguments.Length)
        {
            return null;
        }
        return testAttribute.ConstructorArguments[index];
    }

    /// <summary>
    /// Gets the value of a named argument (property)
    /// </summary>
    public static object? GetNamedArgument(this TestAttributeMetadata testAttribute, string propertyName)
    {
        if (testAttribute.NamedArguments == null || !testAttribute.NamedArguments.TryGetValue(propertyName, out var value))
        {
            return null;
        }
        return value;
    }

    /// <summary>
    /// Tries to get the value of a named argument (property)
    /// </summary>
    public static bool TryGetNamedArgument(this TestAttributeMetadata testAttribute, string propertyName, [NotNullWhen(true)] out object? value)
    {
        value = null;
        if (testAttribute.NamedArguments == null)
        {
            return false;
        }
        return testAttribute.NamedArguments.TryGetValue(propertyName, out value);
    }
}