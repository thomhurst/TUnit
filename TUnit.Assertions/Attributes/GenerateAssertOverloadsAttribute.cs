using System;

namespace TUnit.Assertions.Attributes;

/// <summary>
/// Marks an Assert.That() method for automatic generation of wrapper overloads.
/// Generates Func, Task, and ValueTask variants automatically.
/// </summary>
/// <remarks>
/// <para>
/// When applied to an Assert.That() method, the source generator will create:
/// - Wrapper types (FuncXxxAssertion, TaskXxxAssertion, etc.) implementing IAssertionSource&lt;T&gt;
/// - Assert.That() overloads for Func&lt;T&gt;, Func&lt;Task&lt;T&gt;&gt;, Func&lt;ValueTask&lt;T&gt;&gt;, Task&lt;T&gt;, and ValueTask&lt;T&gt; variants
/// </para>
/// <para>
/// This allows assertions to work seamlessly with lazy evaluation (Func), async operations (Task/ValueTask),
/// and async factories (Func&lt;Task&gt;/Func&lt;ValueTask&gt;).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public static partial class Assert
/// {
///     [GenerateAssertOverloads(Priority = 3)]
///     public static DictionaryAssertion&lt;TKey, TValue&gt; That&lt;TKey, TValue&gt;(
///         IReadOnlyDictionary&lt;TKey, TValue&gt;? value,
///         [CallerArgumentExpression(nameof(value))] string? expression = null)
///     {
///         return new DictionaryAssertion&lt;TKey, TValue&gt;(value, expression);
///     }
/// }
///
/// // Generates overloads like:
/// // Assert.That(Func&lt;IReadOnlyDictionary&lt;TKey, TValue&gt;?&gt; func, ...)
/// // Assert.That(Task&lt;IReadOnlyDictionary&lt;TKey, TValue&gt;?&gt; task, ...)
/// // Assert.That(ValueTask&lt;IReadOnlyDictionary&lt;TKey, TValue&gt;?&gt; valueTask, ...)
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class GenerateAssertOverloadsAttribute : Attribute
{
    /// <summary>
    /// Overload resolution priority for generated overloads.
    /// Higher values take precedence in overload resolution.
    /// </summary>
    /// <remarks>
    /// Use this to control which overload is selected when multiple could match.
    /// When set to a non-zero value, the generated overloads will have the
    /// [OverloadResolutionPriority] attribute applied with this value.
    /// </remarks>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Generate Func&lt;T&gt; overload. Default: true.
    /// </summary>
    /// <remarks>
    /// When true, generates an overload that accepts a Func&lt;T&gt; for lazy evaluation.
    /// The value is only evaluated when the assertion runs.
    /// </remarks>
    public bool Func { get; set; } = true;

    /// <summary>
    /// Generate Func&lt;Task&lt;T&gt;&gt; overload. Default: true.
    /// </summary>
    /// <remarks>
    /// When true, generates an overload that accepts a Func&lt;Task&lt;T&gt;&gt; for async factory evaluation.
    /// Useful when the async operation should be started fresh for each assertion.
    /// </remarks>
    public bool FuncTask { get; set; } = true;

    /// <summary>
    /// Generate Func&lt;ValueTask&lt;T&gt;&gt; overload. Default: true.
    /// </summary>
    /// <remarks>
    /// When true, generates an overload that accepts a Func&lt;ValueTask&lt;T&gt;&gt; for async factory evaluation.
    /// Useful when the async operation should be started fresh for each assertion with ValueTask semantics.
    /// </remarks>
    public bool FuncValueTask { get; set; } = true;

    /// <summary>
    /// Generate Task&lt;T&gt; overload. Default: true.
    /// </summary>
    /// <remarks>
    /// When true, generates an overload that accepts a Task&lt;T&gt; for awaiting an already-started async operation.
    /// </remarks>
    public bool Task { get; set; } = true;

    /// <summary>
    /// Generate ValueTask&lt;T&gt; overload. Default: true.
    /// </summary>
    /// <remarks>
    /// When true, generates an overload that accepts a ValueTask&lt;T&gt; for awaiting an already-started async operation.
    /// </remarks>
    public bool ValueTask { get; set; } = true;
}
