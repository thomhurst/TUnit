using System;

namespace TUnit.Assertions.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class CreateAssertionAttribute : Attribute
{
    public CreateAssertionAttribute(Type targetType, string methodName, AssertionType assertionType)
    {
        TargetType = targetType;
        MethodName = methodName;
        AssertionType = assertionType;
    }

    /// <summary>
    /// Constructor for methods on a different type than the target type.
    /// </summary>
    /// <param name="targetType">The type of the first parameter (what becomes IValueSource&lt;T&gt;)</param>
    /// <param name="containingType">The type that contains the static method</param>
    /// <param name="methodName">The name of the static method</param>
    /// <param name="assertionType">The types of assertions to generate</param>
    public CreateAssertionAttribute(Type targetType, Type containingType, string methodName, AssertionType assertionType)
    {
        TargetType = targetType;
        ContainingType = containingType;
        MethodName = methodName;
        AssertionType = assertionType;
    }

    public Type TargetType { get; }
    public Type? ContainingType { get; }
    public string MethodName { get; }
    public AssertionType AssertionType { get; }
    
    /// <summary>
    /// Optional custom method name override. If not specified, the method name will be derived from the target method name.
    /// </summary>
    public string? CustomMethodName { get; set; }
}

[Flags]
public enum AssertionType
{
    None = 0,
    Is = 1,
    IsNot = 2,
    Has = 4,
    Does = 8,
    DoesNot = 16
}