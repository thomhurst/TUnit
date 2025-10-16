using System.Runtime.CompilerServices;
using TUnit.Assertions.Exceptions;

namespace TUnit.Assertions.Core;

/// <summary>
/// Base class for assertions that preserve their concrete type through And/Or continuations.
/// Uses the Curiously Recurring Template Pattern (CRTP) to enable type-safe method inheritance.
/// </summary>
/// <typeparam name="TValue">The type of value being asserted</typeparam>
/// <typeparam name="TSelf">The concrete assertion type (enables self-typing pattern)</typeparam>
public abstract class SelfTypedAssertion<TValue, TSelf>
    where TSelf : SelfTypedAssertion<TValue, TSelf>
{
    /// <summary>
    /// The assertion context shared by all assertions in this chain.
    /// Contains the evaluation context (value, timing, exceptions) and expression builder (error messages).
    /// </summary>
    protected readonly AssertionContext<TValue> Context;

    /// <summary>
    /// Custom message added via .Because() to explain why the assertion should pass.
    /// </summary>
    private string? _becauseMessage;

    /// <summary>
    /// Wrapped execution for And/Or chaining.
    /// When set, AssertAsync() delegates to this wrapper instead of executing directly.
    /// </summary>
    private SelfTypedAssertion<TValue, TSelf>? _wrappedExecution;

    protected SelfTypedAssertion(AssertionContext<TValue> context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));

        // Auto-detect chaining from context state
        var (previous, combiner) = context.ConsumePendingLinkSelfTyped();
        if (previous != null && previous is TSelf typedPrevious && combiner.HasValue)
        {
            // Create wrapper based on combiner type
            _wrappedExecution = combiner.Value == CombinerType.And
                ? new Chaining.SelfTypedAndAssertion<TValue, TSelf>(typedPrevious, (TSelf)this)
                : new Chaining.SelfTypedOrAssertion<TValue, TSelf>(typedPrevious, (TSelf)this);
        }
    }

    /// <summary>
    /// Implements the specific check logic for this assertion.
    /// </summary>
    protected virtual Task<AssertionResult> CheckAsync(EvaluationMetadata<TValue> metadata)
    {
        throw new NotImplementedException($"{GetType().Name} must override either CheckAsync() or AssertAsync()");
    }

    /// <summary>
    /// Gets a human-readable description of what this assertion expects.
    /// </summary>
    protected abstract string GetExpectation();

    /// <summary>
    /// Internal accessor for the Context.
    /// </summary>
    internal AssertionContext<TValue> InternalContext => Context;

    /// <summary>
    /// Internal accessor for GetExpectation().
    /// </summary>
    internal string InternalGetExpectation() => GetExpectation();

    /// <summary>
    /// Internal accessor for the because message.
    /// </summary>
    internal string? InternalBecauseMessage => _becauseMessage;

    /// <summary>
    /// Adds a custom message explaining why this assertion should pass.
    /// </summary>
    public TSelf Because(string message)
    {
        _becauseMessage = message?.Trim();
        Context.ExpressionBuilder.Append($".Because(\"{message}\")");
        return (TSelf)this;
    }

    /// <summary>
    /// Main assertion execution flow.
    /// </summary>
    public virtual async Task<TValue?> AssertAsync()
    {
        if (_wrappedExecution != null)
        {
            return await _wrappedExecution.AssertAsync();
        }

        return await ExecuteCoreAsync();
    }

    /// <summary>
    /// Executes the core assertion logic without delegation.
    /// </summary>
    internal async Task<TValue?> ExecuteCoreAsync()
    {
        // If this is a composite assertion, delegate to AssertAsync
        if (this is Chaining.SelfTypedAndAssertion<TValue, TSelf> or Chaining.SelfTypedOrAssertion<TValue, TSelf>)
        {
            return await AssertAsync();
        }

        var (value, exception) = await Context.GetAsync();
        var (startTime, endTime) = Context.GetTiming();

        var metadata = new EvaluationMetadata<TValue>(value, exception, startTime, endTime);
        var result = await CheckAsync(metadata);

        if (!result.IsPassed)
        {
            var assertionException = CreateException(result);
            var currentScope = AssertionScope.GetCurrentAssertionScope();

            if (currentScope != null)
            {
                currentScope.AddException((AssertionException)assertionException);
            }
            else
            {
                throw assertionException;
            }
        }

        return value;
    }

    /// <summary>
    /// Enables await syntax on assertions.
    /// </summary>
    public TaskAwaiter<TValue?> GetAwaiter() => AssertAsync().GetAwaiter();

    /// <summary>
    /// Creates an And continuation that preserves the assertion type.
    /// All instance methods remain available through the continuation.
    /// </summary>
    public virtual SelfTypedAndContinuation<TValue, TSelf> And
    {
        get
        {
            // Check if we're mixing combiners
            if (_wrappedExecution is Chaining.SelfTypedOrAssertion<TValue, TSelf>)
            {
                throw new MixedAndOrAssertionsException();
            }
            return new SelfTypedAndContinuation<TValue, TSelf>(Context, (TSelf)(_wrappedExecution ?? this));
        }
    }

    /// <summary>
    /// Creates an Or continuation that preserves the assertion type.
    /// All instance methods remain available through the continuation.
    /// </summary>
    public virtual SelfTypedOrContinuation<TValue, TSelf> Or
    {
        get
        {
            // Check if we're mixing combiners
            if (_wrappedExecution is Chaining.SelfTypedAndAssertion<TValue, TSelf>)
            {
                throw new MixedAndOrAssertionsException();
            }
            return new SelfTypedOrContinuation<TValue, TSelf>(Context, (TSelf)(_wrappedExecution ?? this));
        }
    }

    /// <summary>
    /// Creates an AssertionException with a formatted error message.
    /// </summary>
    protected Exception CreateException(AssertionResult result)
    {
        var message = $"""
            Expected {GetExpectation()}
            but {result.Message}

            at {Context.ExpressionBuilder}
            """;

        if (_becauseMessage != null)
        {
            var becausePrefix = _becauseMessage.StartsWith("because ", StringComparison.OrdinalIgnoreCase)
                ? _becauseMessage
                : $"because {_becauseMessage}";
            message += $"\n\n{becausePrefix}";
        }

        return new AssertionException(message);
    }
}
