using System;

namespace TUnit.Assertions.Should.Attributes;

/// <summary>
/// Overrides the auto-conjugated Should name produced by the source generator for a class
/// decorated with <c>[AssertionExtension]</c>. Use when the conjugation rules don't produce
/// the desired Should-flavored name.
/// </summary>
/// <remarks>
/// The override applies to the extension method whose name matches the class's
/// <c>[AssertionExtension(MethodName)]</c>. Negated forms (declared via
/// <c>[AssertionExtension(NegatedMethodName = "...")]</c>) are emitted as separate methods by
/// <c>TUnit.Assertions.SourceGenerator</c> and get their own auto-conjugation pass — to override
/// a negated name, place a separate <c>[ShouldName]</c> on the negated assertion class (TUnit's
/// own naming convention typically gives positive and negated assertions distinct classes).
/// </remarks>
/// <example>
/// <code>
/// [AssertionExtension("IsOdd")]
/// [ShouldName("BeOdd")]
/// public class IsOddAssertion : Assertion&lt;int&gt; { ... }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class ShouldNameAttribute : Attribute
{
    public ShouldNameAttribute(string name) => Name = name;

    /// <summary>The Should-flavored method name to emit (overrides conjugation).</summary>
    public string Name { get; }
}
