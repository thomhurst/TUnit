using System;

namespace TUnit.Assertions.Attributes;

/// <summary>
/// Marks an assertion class to have extension methods automatically generated based on its constructors.
/// The generator will create extension methods for IAssertionSource&lt;T&gt; that construct instances of this assertion.
/// Each public constructor (that takes AssertionContext&lt;T&gt; as the first parameter) will generate a corresponding extension method.
/// </summary>
/// <example>
/// <code>
/// [AssertionExtension("IsLetter")]
/// public class IsLetterAssertion : Assertion&lt;char&gt;
/// {
///     public IsLetterAssertion(AssertionContext&lt;char&gt; context) : base(context) { }
///     // Generates: public static IsLetterAssertion IsLetter(this IAssertionSource&lt;char&gt; source)
/// }
///
/// [AssertionExtension("IsEqualTo")]
/// public class EqualsAssertion&lt;TValue&gt; : Assertion&lt;TValue&gt;
/// {
///     public EqualsAssertion(AssertionContext&lt;TValue&gt; context, TValue expected) : base(context) { }
///     // Generates: public static EqualsAssertion&lt;TValue&gt; IsEqualTo&lt;TValue&gt;(this IAssertionSource&lt;TValue&gt; source, TValue expected, [CallerArgumentExpression] string? expression = null)
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class AssertionExtensionAttribute : Attribute
{
    /// <summary>
    /// Creates a new AssertionExtensionAttribute with the specified method name.
    /// </summary>
    /// <param name="methodName">The name of the extension method to generate (e.g., "IsEqualTo", "Contains")</param>
    public AssertionExtensionAttribute(string methodName)
    {
        MethodName = methodName;
    }

    /// <summary>
    /// The name of the extension method to generate.
    /// This will be used as the method name in the generated extension.
    /// </summary>
    public string MethodName { get; }

    /// <summary>
    /// Optional: The name of a negated version of this assertion to generate.
    /// If specified, the generator will create an additional extension method with this name
    /// that passes an additional "negated: true" parameter to the constructor.
    /// </summary>
    /// <example>
    /// [AssertionExtension("Contains", NegatedMethodName = "DoesNotContain")]
    /// </example>
    public string? NegatedMethodName { get; set; }

    /// <summary>
    /// Optional: The overload resolution priority for the generated extension methods.
    /// Higher values are preferred over lower values during overload resolution.
    /// Default is 0. When 0, no OverloadResolutionPriority attribute is generated.
    /// Use higher values (e.g., 2) for specialized overloads that should take precedence.
    /// </summary>
    /// <example>
    /// [AssertionExtension("IsEqualTo", OverloadResolutionPriority = 2)]
    /// </example>
    public int OverloadResolutionPriority { get; set; } = 0;
}
