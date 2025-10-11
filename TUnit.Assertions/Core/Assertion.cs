using System.Runtime.CompilerServices;
using System.Text;
using TUnit.Assertions.Exceptions;

namespace TUnit.Assertions.Core;

/// <summary>
/// Base class for all assertions in the new architecture.
/// Each assertion represents a single check that can be chained with others.
/// All assertions in a chain share the same AssertionContext.
/// </summary>
///
/// Note: This class does NOT implement IAssertionSource to enforce type-safe chaining.
/// Only sources (ValueAssertion, CollectionAssertion, etc.) and continuations (AndContinuation,
/// OrContinuation) implement IAssertionSource. This design prevents direct chaining of assertions
/// without And/Or keywords and prevents awaiting bare sources without an assertion.
/// <typeparam name="TValue">The type of value being asserted</typeparam>
public abstract class Assertion<TValue>
{
    /// <summary>
    /// The assertion context shared by all assertions in this chain.
    /// Contains the evaluation context (value, timing, exceptions) and expression builder (error messages).
    /// </summary>
    protected readonly AssertionContext<TValue> Context;

    /// <summary>
    /// Internal accessor for the context, used by And/OrAssertion to access context from assertion parameters.
    /// </summary>
    internal AssertionContext<TValue> InternalContext => Context;

    /// <summary>
    /// Custom message added via .Because() to explain why the assertion should pass.
    /// </summary>
    private string? _becauseMessage;


    protected Assertion(AssertionContext<TValue> context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Implements the specific check logic for this assertion.
    /// Called after the context has been evaluated.
    /// Override this method if your assertion uses the default AssertAsync() flow.
    /// If you override AssertAsync() with custom logic (like AndAssertion/OrAssertion), you don't need to implement this.
    /// </summary>
    /// <param name="metadata">Metadata about the evaluation including value, exception, and timing information</param>
    /// <returns>The result of the assertion check</returns>
    protected virtual Task<AssertionResult> CheckAsync(EvaluationMetadata<TValue> metadata)
    {
        throw new NotImplementedException($"{GetType().Name} must override either CheckAsync() or AssertAsync()");
    }

    /// <summary>
    /// Gets a human-readable description of what this assertion expects.
    /// Used in error messages.
    /// </summary>
    protected abstract string GetExpectation();

    /// <summary>
    /// Internal accessor for GetExpectation(), used by And/OrAssertion to build combined error messages.
    /// </summary>
    internal string InternalGetExpectation() => GetExpectation();

    /// <summary>
    /// Internal accessor for the because message, used by And/OrAssertion to build combined error messages.
    /// </summary>
    internal string? InternalBecauseMessage => _becauseMessage;

    /// <summary>
    /// Adds a custom message explaining why this assertion should pass.
    /// This message will be included in the error message if the assertion fails.
    /// </summary>
    public Assertion<TValue> Because(string message)
    {
        // Trim whitespace from the message
        _becauseMessage = message?.Trim();
        Context.ExpressionBuilder.Append($".Because(\"{message}\")");
        return this;
    }

    /// <summary>
    /// Main assertion execution flow.
    /// Evaluates the context (if not already evaluated), checks the condition,
    /// and throws if the assertion fails (or adds to AssertionScope if within Assert.Multiple).
    /// </summary>
    public virtual async Task<TValue?> AssertAsync()
    {
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
                // Within Assert.Multiple - accumulate exception instead of throwing
                currentScope.AddException((AssertionException)assertionException);
            }
            else
            {
                // No scope - throw immediately
                throw assertionException;
            }
        }

        return value;
    }

    /// <summary>
    /// Enables await syntax on assertions.
    /// Example: await Assert.That(value).IsEqualTo(5);
    /// </summary>
    public TaskAwaiter<TValue?> GetAwaiter() => AssertAsync().GetAwaiter();

    /// <summary>
    /// Creates an And continuation for chaining additional assertions.
    /// All assertions in an And chain must pass.
    /// </summary>
    public AndContinuation<TValue> And => new(Context, this);

    /// <summary>
    /// Creates an Or continuation for chaining alternative assertions.
    /// At least one assertion in an Or chain must pass.
    /// </summary>
    public OrContinuation<TValue> Or => new(Context, this);

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
            // Check if message already starts with "because" to avoid duplication
            var becausePrefix = _becauseMessage.StartsWith("because ", StringComparison.OrdinalIgnoreCase)
                ? _becauseMessage
                : $"because {_becauseMessage}";
            message += $"\n\n{becausePrefix}";
        }

        return new AssertionException(message);
    }

    /// <summary>
    /// Appends an expression fragment to the expression builder.
    /// Used by extension methods to build the full assertion expression.
    /// </summary>
    protected internal void AppendExpression(string expression)
    {
        if (!string.IsNullOrEmpty(expression))
        {
            Context.ExpressionBuilder.Append(expression);
        }
    }
}
