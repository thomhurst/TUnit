using System;

namespace TUnit.Assertions.Attributes;

/// <summary>
/// Marks a method to have assertion infrastructure automatically generated.
/// The method becomes usable as a fluent assertion in the Assert.That() chain.
/// </summary>
/// <remarks>
/// <para>
/// The decorated method must be static and return one of:
/// - bool
/// - AssertionResult
/// - Task&lt;bool&gt;
/// - Task&lt;AssertionResult&gt;
/// </para>
/// <para>
/// The first parameter determines the target type (what becomes IAssertionSource&lt;T&gt;).
/// Additional parameters become parameters of the generated extension method.
/// </para>
/// <para>
/// The generator creates:
/// - An Assertion&lt;T&gt; class containing the assertion logic
/// - An extension method on IAssertionSource&lt;T&gt; that constructs the assertion
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [GenerateAssertion]
/// public static bool IsPositive(this int value)
///     => value > 0;
///
/// // Generates extension method:
/// // public static IsPositive_Assertion IsPositive(this IAssertionSource&lt;int&gt; source)
///
/// // Usage:
/// await Assert.That(5).IsPositive();
/// </code>
/// </example>
/// <example>
/// <code>
/// [GenerateAssertion]
/// public static bool IsGreaterThan(this int value, int threshold)
///     => value > threshold;
///
/// // Generates extension method with parameter:
/// // public static IsGreaterThan_Int_Assertion IsGreaterThan(
/// //     this IAssertionSource&lt;int&gt; source,
/// //     int threshold,
/// //     [CallerArgumentExpression(nameof(threshold))] string? expr = null)
///
/// // Usage:
/// await Assert.That(10).IsGreaterThan(5);
/// </code>
/// </example>
/// <example>
/// <code>
/// [GenerateAssertion]
/// public static AssertionResult IsPrime(this int value)
/// {
///     if (value &lt; 2)
///         return AssertionResult.Failed($"{value} is less than 2");
///     for (int i = 2; i &lt;= Math.Sqrt(value); i++)
///     {
///         if (value % i == 0)
///             return AssertionResult.Failed($"{value} is divisible by {i}");
///     }
///     return AssertionResult.Passed;
/// }
///
/// // Usage:
/// await Assert.That(17).IsPrime();
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class GenerateAssertionAttribute : Attribute
{
}
