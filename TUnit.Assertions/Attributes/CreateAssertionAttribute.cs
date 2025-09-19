using System;

namespace TUnit.Assertions.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class CreateAssertionAttribute : Attribute
{
    public CreateAssertionAttribute(Type targetType, string methodName)
    {
        TargetType = targetType;
        MethodName = methodName;
    }

    /// <summary>
    /// Constructor for methods on a different type than the target type.
    /// </summary>
    /// <param name="targetType">The type of the first parameter (what becomes IValueSource&lt;T&gt;)</param>
    /// <param name="containingType">The type that contains the static method</param>
    /// <param name="methodName">The name of the static method</param>
    /// <param name="assertionGenerationType">The types of assertions to generate</param>
    public CreateAssertionAttribute(Type targetType, Type containingType, string methodName)
    {
        TargetType = targetType;
        ContainingType = containingType;
        MethodName = methodName;
    }

    public Type TargetType { get; }
    public Type? ContainingType { get; }
    public string MethodName { get; }


    /// <summary>
    /// Optional custom name for the generated assertion method. If not specified, the name will be derived from the target method name.
    /// </summary>
    public string? CustomName { get; set; }

    /// <summary>
    /// When true, inverts the logic of the assertion. This is used for creating negative assertions (e.g., DoesNotContain from Contains).
    /// The generated assertion will negate the result of the target method.
    /// </summary>
    public bool NegateLogic { get; set; }

    /// <summary>
    /// Indicates if this method requires generic type parameter handling (e.g., Enum.IsDefined(Type, object) where Type becomes typeof(T)).
    /// </summary>
    public bool RequiresGenericTypeParameter { get; set; }
    
    /// <summary>
    /// When true, treats the method as an instance method even if it's static (useful for extension methods).
    /// When false (default), the generator will automatically determine based on the method's actual signature.
    /// </summary>
    public bool TreatAsInstance { get; set; }
}
