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

    public AssertionContext<TException> MapException<TException>() where TException : Exception
    {
        return new AssertionContext<TException>(
            Evaluation.MapException<TException>(),
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

    /// <summary>
    /// Pending assertion to link with when the next assertion is constructed.
    /// Set by AndContinuation/OrContinuation, consumed by Assertion constructor.
    /// </summary>
    internal Assertion<TValue>? PendingLinkPrevious { get; private set; }

    /// <summary>
    /// The type of combiner (And/Or) for the pending link.
    /// </summary>
    internal CombinerType? PendingLinkType { get; private set; }

    /// <summary>
    /// Pending assertion to link with for self-typed assertions.
    /// Stored as object to avoid type parameter explosion.
    /// </summary>
    internal object? PendingLinkPreviousSelfTyped { get; private set; }

    /// <summary>
    /// Sets the pending link state for the next assertion to consume.
    /// Called by AndContinuation/OrContinuation constructors.
    /// </summary>
    internal void SetPendingLink(Assertion<TValue> previous, CombinerType type)
    {
        PendingLinkPrevious = previous;
        PendingLinkType = type;
    }

    /// <summary>
    /// Sets the pending link state for self-typed assertions.
    /// Called by SelfTypedAndContinuation/SelfTypedOrContinuation constructors.
    /// </summary>
    internal void SetPendingLinkSelfTyped(object previous, CombinerType type)
    {
        PendingLinkPreviousSelfTyped = previous;
        PendingLinkType = type;
    }

    /// <summary>
    /// Consumes and clears the pending link state.
    /// Called by Assertion constructor to auto-detect chaining.
    /// </summary>
    internal (Assertion<TValue>? previous, CombinerType? type) ConsumePendingLink()
    {
        var result = (PendingLinkPrevious, PendingLinkType);
        PendingLinkPrevious = null;
        PendingLinkType = null;
        return result;
    }

    /// <summary>
    /// Consumes and clears the self-typed pending link state.
    /// Called by SelfTypedAssertion constructor to auto-detect chaining.
    /// </summary>
    internal (object? previous, CombinerType? type) ConsumePendingLinkSelfTyped()
    {
        var result = (PendingLinkPreviousSelfTyped, PendingLinkType);
        PendingLinkPreviousSelfTyped = null;
        PendingLinkType = null;
        return result;
    }
}
