using System;

namespace TUnit.Assertions.Attributes;

/// <summary>
/// Generates assertion infrastructure from an existing method (instance or static).
/// This is the renamed and enhanced version of CreateAssertionAttribute.
/// </summary>
/// <remarks>
/// <para>
/// Use this attribute on a partial class to generate assertions from existing methods.
/// The target method can be on the same type or a different type.
/// </para>
/// <para>
/// Supported method return types:
/// - bool
/// - AssertionResult
/// - Task&lt;bool&gt;
/// - Task&lt;AssertionResult&gt;
/// </para>
/// <para>
/// The generator creates:
/// - An Assertion&lt;T&gt; class that calls the target method
/// - An extension method on IAssertionSource&lt;T&gt;
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Reference instance methods on the target type
/// [AssertionFrom(typeof(string), "StartsWith")]
/// [AssertionFrom(typeof(string), "EndsWith")]
/// [AssertionFrom(typeof(string), "Contains")]
/// public static partial class StringAssertionExtensions { }
///
/// // Usage:
/// await Assert.That("hello").StartsWith("he");
/// </code>
/// </example>
/// <example>
/// <code>
/// // Reference static helper methods
/// [AssertionFrom(typeof(string), typeof(string), "IsNullOrEmpty")]
/// public static partial class StringAssertionExtensions { }
///
/// // Usage:
/// await Assert.That(myString).IsNullOrEmpty();
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class AssertionFromAttribute : Attribute
{
    /// <summary>
    /// Creates an assertion from a method on the target type itself.
    /// </summary>
    /// <param name="targetType">The type of the value being asserted (becomes IAssertionSource&lt;T&gt;)</param>
    /// <param name="methodName">The name of the method to call</param>
    public AssertionFromAttribute(Type targetType, string methodName)
    {
        TargetType = targetType ?? throw new ArgumentNullException(nameof(targetType));
        MethodName = methodName ?? throw new ArgumentNullException(nameof(methodName));
    }

    /// <summary>
    /// Creates an assertion from a method on a different type than the target.
    /// </summary>
    /// <param name="targetType">The type of the value being asserted (becomes IAssertionSource&lt;T&gt;)</param>
    /// <param name="containingType">The type that contains the method</param>
    /// <param name="methodName">The name of the method to call</param>
    public AssertionFromAttribute(Type targetType, Type containingType, string methodName)
    {
        TargetType = targetType ?? throw new ArgumentNullException(nameof(targetType));
        ContainingType = containingType ?? throw new ArgumentNullException(nameof(containingType));
        MethodName = methodName ?? throw new ArgumentNullException(nameof(methodName));
    }

    /// <summary>
    /// The type of the value being asserted. This becomes the T in IAssertionSource&lt;T&gt;.
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
    /// [AssertionFrom(typeof(string), "Contains", CustomName = "Has")]
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
    /// [AssertionFrom(typeof(string), "Contains", CustomName = "DoesNotContain", NegateLogic = true)]
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
}
