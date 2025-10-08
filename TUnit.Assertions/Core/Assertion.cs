using System.Runtime.CompilerServices;
using System.Text;
using TUnit.Assertions.Exceptions;

namespace TUnit.Assertions.Core;

/// <summary>
/// Base class for all assertions in the new architecture.
/// Each assertion represents a single check that can be chained with others.
/// All assertions in a chain share the same EvaluationContext.
/// </summary>
/// <typeparam name="TValue">The type of value being asserted</typeparam>
public abstract class Assertion<TValue> : IAssertionSource<TValue>
{
    /// <summary>
    /// The evaluation context shared by all assertions in this chain.
    /// Handles lazy evaluation and caching of the source value.
    /// </summary>
    protected readonly EvaluationContext<TValue> Context;

    /// <summary>
    /// Expression builder that captures the assertion chain for error messages.
    /// Shared and mutated as assertions are chained together.
    /// </summary>
    protected readonly StringBuilder ExpressionBuilder;

    /// <summary>
    /// Custom message added via .Because() to explain why the assertion should pass.
    /// </summary>
    private string? _becauseMessage;

    // Explicit interface implementation
    EvaluationContext<TValue> IAssertionSource<TValue>.Context => Context;
    StringBuilder IAssertionSource<TValue>.ExpressionBuilder => ExpressionBuilder;

    protected Assertion(EvaluationContext<TValue> context, StringBuilder? expressionBuilder = null)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        ExpressionBuilder = expressionBuilder ?? new StringBuilder();
    }

    /// <summary>
    /// Implements the specific check logic for this assertion.
    /// Called after the context has been evaluated.
    /// Override this method if your assertion uses the default AssertAsync() flow.
    /// If you override AssertAsync() with custom logic (like AndAssertion/OrAssertion), you don't need to implement this.
    /// </summary>
    /// <param name="value">The evaluated value (may be null)</param>
    /// <param name="exception">Any exception that occurred during evaluation (may be null)</param>
    /// <returns>The result of the assertion check</returns>
    protected virtual Task<AssertionResult> CheckAsync(TValue? value, Exception? exception)
    {
        throw new NotImplementedException($"{GetType().Name} must override either CheckAsync() or AssertAsync()");
    }

    /// <summary>
    /// Gets a human-readable description of what this assertion expects.
    /// Used in error messages.
    /// </summary>
    protected abstract string GetExpectation();

    /// <summary>
    /// Adds a custom message explaining why this assertion should pass.
    /// This message will be included in the error message if the assertion fails.
    /// </summary>
    public Assertion<TValue> Because(string message)
    {
        _becauseMessage = message;
        ExpressionBuilder.Append($".Because(\"{message}\")");
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

        var result = await CheckAsync(value, exception);

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
    public AndContinuation<TValue> And => new(Context, ExpressionBuilder);

    /// <summary>
    /// Creates an Or continuation for chaining alternative assertions.
    /// At least one assertion in an Or chain must pass.
    /// </summary>
    public OrContinuation<TValue> Or => new(Context, ExpressionBuilder);

    /// <summary>
    /// Creates an AssertionException with a formatted error message.
    /// </summary>
    protected Exception CreateException(AssertionResult result)
    {
        var timing = Context.GetTiming();
        var duration = timing.End - timing.Start;

        var message = $"""
            Expected {GetExpectation()}
            but {result.Message}

            at {ExpressionBuilder}
            """;

        if (_becauseMessage != null)
        {
            message += $"\n\nbecause {_becauseMessage}";
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
            ExpressionBuilder.Append(expression);
        }
    }
}
