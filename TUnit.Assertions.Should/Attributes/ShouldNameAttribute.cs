using System;

namespace TUnit.Assertions.Should.Attributes;

/// <summary>
/// Overrides the auto-conjugated Should name produced by the source generator
/// for a class decorated with <c>[AssertionExtension]</c>. Use when the
/// conjugation rules don't produce the desired Should-flavored name.
/// </summary>
/// <example>
/// <code>
/// [AssertionExtension("IsOdd", NegatedMethodName = "IsNotOdd")]
/// [ShouldName("BeOdd", Negated = "NotBeOdd")]
/// public class IsOddAssertion : Assertion&lt;int&gt; { ... }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class ShouldNameAttribute : Attribute
{
    public ShouldNameAttribute(string name) => Name = name;

    /// <summary>The Should-flavored method name to emit (overrides conjugation).</summary>
    public string Name { get; }

    /// <summary>The Should-flavored negated method name to emit (overrides conjugation of <c>NegatedMethodName</c>).</summary>
    public string? Negated { get; set; }
}
