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
/// - AssertionResult&lt;T&gt; (terminal assertion — returns T when awaited)
/// - Task&lt;bool&gt;
/// - Task&lt;AssertionResult&gt;
/// - Task&lt;AssertionResult&lt;T&gt;&gt; (terminal assertion — returns T when awaited)
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
/// <example>
/// <code>
/// [GenerateAssertion(ExpectationMessage = "to contain message '{needle}'")]
/// public static AssertionResult&lt;string&gt; ContainsMessage(this IEnumerable&lt;string&gt; strings, string needle)
/// {
///     var result = strings.FirstOrDefault(x =&gt; x.Contains(needle));
///     if (result is not null)
///         return AssertionResult&lt;string&gt;.Passed(result);
///     return AssertionResult.Failed($"{needle} not found");
/// }
///
/// // Usage (terminal assertion — returns the matched item):
/// string matched = await Assert.That(items).ContainsMessage("foo");
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class GenerateAssertionAttribute : Attribute
{
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
    /// [GenerateAssertion(ExpectationMessage = "be greater than {threshold}")]
    /// public static bool IsGreaterThan(this int value, int threshold)
    ///     => value > threshold;
    ///
    /// // Error message: "Expected 3 to be greater than 5 but found 3"
    /// </code>
    /// </example>
    public string? ExpectationMessage { get; set; }

    /// <summary>
    /// When true, the method body will be inlined into the generated assertion instead of calling the method.
    /// This removes the need for the method to be visible and eliminates the need for [EditorBrowsable] attributes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Inlining is recommended when defining assertion helpers in file-scoped classes,
    /// as it allows the helper methods to remain private while still generating public assertions.
    /// </para>
    /// <para>
    /// The source generator will fully qualify all type references to ensure the inlined code
    /// works correctly regardless of namespace context.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// file static class BoolAssertions
    /// {
    ///     [GenerateAssertion(InlineMethodBody = true)]
    ///     public static bool IsTrue(this bool value) => value == true;
    /// }
    /// // No need for [EditorBrowsable], class can be file-scoped
    /// </code>
    /// </example>
    public bool InlineMethodBody { get; set; }
}
