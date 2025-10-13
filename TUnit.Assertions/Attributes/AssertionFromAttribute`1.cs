using System;

namespace TUnit.Assertions.Attributes;

/// <summary>
/// Generic version of AssertionFromAttribute that provides better type safety and cleaner syntax.
/// This is the renamed and enhanced version of CreateAssertionAttribute&lt;T&gt;.
/// </summary>
/// <typeparam name="TTarget">The target type for the assertion (becomes IAssertionSource&lt;TTarget&gt;)</typeparam>
/// <remarks>
/// <para>
/// Use this attribute on a partial class to generate assertions from existing methods.
/// The generic type parameter specifies the type being asserted.
/// </para>
/// <para>
/// Supported method return types:
/// - bool
/// - AssertionResult
/// - Task&lt;bool&gt;
/// - Task&lt;AssertionResult&gt;
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Reference instance methods
/// [AssertionFrom&lt;string&gt;("StartsWith")]
/// [AssertionFrom&lt;string&gt;("EndsWith")]
/// public static partial class StringAssertionExtensions { }
///
/// // Usage:
/// await Assert.That("hello").StartsWith("he");
/// </code>
/// </example>
/// <example>
/// <code>
/// // Reference methods on a different type
/// [AssertionFrom&lt;string&gt;(typeof(StringHelper), "IsValid")]
/// public static partial class StringAssertionExtensions { }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class AssertionFromAttribute<TTarget> : Attribute
{
    /// <summary>
    /// Creates an assertion from a method on the target type itself.
    /// </summary>
    /// <param name="methodName">The name of the method to call</param>
    public AssertionFromAttribute(string methodName)
    {
        TargetType = typeof(TTarget);
        MethodName = methodName ?? throw new ArgumentNullException(nameof(methodName));
    }

    /// <summary>
    /// Creates an assertion from a method on a different type than the target.
    /// </summary>
    /// <param name="containingType">The type that contains the method</param>
    /// <param name="methodName">The name of the method to call</param>
    public AssertionFromAttribute(Type containingType, string methodName)
    {
        TargetType = typeof(TTarget);
        ContainingType = containingType ?? throw new ArgumentNullException(nameof(containingType));
        MethodName = methodName ?? throw new ArgumentNullException(nameof(methodName));
    }

    /// <summary>
    /// The type of the value being asserted. Derived from the generic type parameter.
    /// </summary>
    public Type TargetType { get; }

    /// <summary>
    /// The type that contains the method. If null, the method is assumed to be on the target type.
    /// </summary>
    public Type? ContainingType { get; }

    /// <summary>
    /// The name of the method to call for the assertion logic.
    /// </summary>
    public string MethodName { get; }

    /// <summary>
    /// Optional custom name for the generated assertion method.
    /// If not specified, the method name will be used.
    /// </summary>
    /// <example>
    /// <code>
    /// [AssertionFrom&lt;string&gt;("Contains", CustomName = "Has")]
    /// // Generates .Has() instead of .Contains()
    /// </code>
    /// </example>
    public string? CustomName { get; set; }

    /// <summary>
    /// When true, inverts the logic of the assertion for bool-returning methods.
    /// This is used for creating negative assertions (e.g., DoesNotContain from Contains).
    /// </summary>
    /// <remarks>
    /// Only applies to methods returning bool or Task&lt;bool&gt;.
    /// Methods returning AssertionResult cannot be negated (the result already determines pass/fail).
    /// A warning will be issued if NegateLogic is set for AssertionResult methods.
    /// </remarks>
    /// <example>
    /// <code>
    /// [AssertionFrom&lt;string&gt;("Contains", CustomName = "DoesNotContain", NegateLogic = true)]
    /// // Generates DoesNotContain() that passes when Contains returns false
    /// </code>
    /// </example>
    public bool NegateLogic { get; set; }

    /// <summary>
    /// Indicates if this method requires generic type parameter handling.
    /// Example: Enum.IsDefined(Type, object) where Type becomes typeof(T).
    /// </summary>
    public bool RequiresGenericTypeParameter { get; set; }

    /// <summary>
    /// When true, treats a static method as if it were an instance method.
    /// The first parameter becomes the value being asserted.
    /// When false (default), the generator automatically determines the pattern.
    /// </summary>
    public bool TreatAsInstance { get; set; }

    /// <summary>
    /// Optional custom expectation message for the assertion.
    /// If not specified, a default message will be generated based on the method name and parameters.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The expectation message is used in error messages when the assertion fails.
    /// It appears as: "Expected {value} to {expectation} but {actual_result}".
    /// </para>
    /// <para>
    /// You can use parameter placeholders like {param1}, {param2} which will be replaced with actual values.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// [AssertionFrom&lt;string&gt;("StartsWith", ExpectationMessage = "start with {value}")]
    /// // Error message: "Expected 'hello' to start with 'xyz' but..."
    /// </code>
    /// </example>
    public string? ExpectationMessage { get; set; }
}
