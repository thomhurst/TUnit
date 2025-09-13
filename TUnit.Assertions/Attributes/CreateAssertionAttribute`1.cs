using System;

namespace TUnit.Assertions.Attributes;

/// <summary>
/// Generic version of CreateAssertionAttribute that provides better type safety and cleaner syntax.
/// Use like: [CreateAssertion&lt;char&gt;("IsDigit")]
/// </summary>
/// <typeparam name="TTarget">The target type for the assertion</typeparam>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class CreateAssertionAttribute<TTarget> : Attribute
{
    public CreateAssertionAttribute(string methodName)
    {
        TargetType = typeof(TTarget);
        MethodName = methodName;
    }

    /// <summary>
    /// Constructor for methods on a different type than the target type.
    /// </summary>
    /// <param name="containingType">The type that contains the static method</param>
    /// <param name="methodName">The name of the static method</param>
    public CreateAssertionAttribute(Type containingType, string methodName)
    {
        TargetType = typeof(TTarget);
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