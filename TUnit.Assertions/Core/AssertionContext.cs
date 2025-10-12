using System.Text;

namespace TUnit.Assertions.Core;

/// <summary>
/// Contains the shared state for an assertion chain.
/// Combines evaluation context (value, timing, exceptions) with expression building (error messages).
/// All assertions in a chain share the same AssertionContext instance.
/// </summary>
/// <typeparam name="TValue">The type of value being asserted</typeparam>
public sealed class AssertionContext<TValue>
{
    /// <summary>
    /// Handles lazy evaluation, caching, and exception capture for the source value.
    /// </summary>
    public EvaluationContext<TValue> Evaluation { get; }

    /// <summary>
    /// Builds the assertion chain expression for error messages.
    /// Mutated as assertions are chained together.
    /// </summary>
    public StringBuilder ExpressionBuilder { get; }

    /// <summary>
    /// Creates a new assertion context with the given evaluation context and expression builder.
    /// </summary>
    public AssertionContext(EvaluationContext<TValue> evaluation, StringBuilder expressionBuilder)
    {
        Evaluation = evaluation ?? throw new ArgumentNullException(nameof(evaluation));
        ExpressionBuilder = expressionBuilder ?? throw new ArgumentNullException(nameof(expressionBuilder));
    }

    /// <summary>
    /// Creates a new assertion context for immediate values (no evaluation needed).
    /// </summary>
    public AssertionContext(TValue? value, StringBuilder expressionBuilder)
    {
        Evaluation = new EvaluationContext<TValue>(value);
        ExpressionBuilder = expressionBuilder ?? throw new ArgumentNullException(nameof(expressionBuilder));
    }

    /// <summary>
    /// Creates a derived context by mapping the value to a different type.
    /// Used for type transformations like IsTypeOf&lt;T&gt;().
    /// The mapping function is only called if evaluation succeeds (no exception).
    /// </summary>
    public AssertionContext<TNew> Map<TNew>(Func<TValue?, TNew?> mapper)
    {
        return new AssertionContext<TNew>(
            Evaluation.Map(mapper),
            ExpressionBuilder
        );
    }

    /// <summary>
    /// Gets the evaluated value and any exception that occurred.
    /// Evaluates once and caches the result for subsequent calls.
    /// </summary>
    public Task<(TValue? Value, Exception? Exception)> GetAsync()
    {
        return Evaluation.GetAsync();
    }

    /// <summary>
    /// Gets the timing information for this evaluation.
    /// Only meaningful after evaluation has occurred.
    /// </summary>
    public (DateTimeOffset Start, DateTimeOffset End) GetTiming()
    {
        return Evaluation.GetTiming();
    }
}
