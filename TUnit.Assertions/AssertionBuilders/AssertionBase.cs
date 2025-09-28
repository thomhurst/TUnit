using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.Exceptions;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// Simplified base class for all assertions with lazy evaluation
/// </summary>
public abstract class AssertionBase<T> : AssertionBase
{
    private readonly Func<Task<T>> _actualValueProvider;
    private readonly List<(ChainType type, AssertionBase<T> assertion)> _chainedAssertions = new();
    private ChainType _nextChainType = ChainType.None;
    private string? _becauseReason;
    private string? _becauseExpression;

    // Support both sync and async value providers
    protected AssertionBase(T actualValue)
        : this(() => Task.FromResult(actualValue)) { }

    protected AssertionBase(Func<T> actualValueProvider)
        : this(() => Task.FromResult(actualValueProvider())) { }

    protected AssertionBase(Task<T> actualValueTask)
        : this(() => actualValueTask) { }

    protected AssertionBase(Func<Task<T>> actualValueProvider)
    {
        _actualValueProvider = actualValueProvider ?? throw new ArgumentNullException(nameof(actualValueProvider));
    }

    /// <summary>
    /// Gets the actual value - ONLY called during execution
    /// </summary>
    protected async Task<T> GetActualValueAsync()
    {
        return await _actualValueProvider();
    }

    /// <summary>
    /// Configuration method - NO EVALUATION
    /// </summary>
    public virtual AssertionBase<T> Because(string reason, [CallerArgumentExpression(nameof(reason))] string? expression = null)
    {
        _becauseReason = reason;
        _becauseExpression = expression;
        return this;
    }

    /// <summary>
    /// Sets up an AND chain - NO EVALUATION
    /// Returns an AssertionBuilder to enable further assertions
    /// </summary>
    public AssertionBuilder<T> And
    {
        get
        {
            // Return a new AssertionBuilder that will first execute this assertion
            // then allow chaining additional assertions
            return new AssertionBuilder<T>(async () =>
            {
                await ExecuteAsync(); // Execute this assertion first
                return await GetActualValueAsync(); // Return the value for next assertion
            });
        }
    }

    /// <summary>
    /// Sets up an OR chain - NO EVALUATION
    /// </summary>
    public AssertionBuilder<T> Or
    {
        get
        {
            // Return a new AssertionBuilder for OR chaining
            // Note: OR logic would need more complex handling
            return new AssertionBuilder<T>(async () =>
            {
                try
                {
                    await ExecuteAsync(); // Try this assertion
                    return await GetActualValueAsync();
                }
                catch
                {
                    // If this fails, allow next assertion
                    return await GetActualValueAsync();
                }
            });
        }
    }

    /// <summary>
    /// Chains another assertion - NO EVALUATION
    /// </summary>
    internal AssertionBase<T> Chain(AssertionBase<T> nextAssertion)
    {
        var chainType = _nextChainType == ChainType.None ? ChainType.And : _nextChainType;
        _chainedAssertions.Add((chainType, nextAssertion));
        _nextChainType = ChainType.None;
        return this;
    }

    /// <summary>
    /// LAZY EXECUTION - Only runs when awaited
    /// </summary>
    public override async Task ExecuteAsync()
    {
        try
        {
            // Execute the main assertion
            var result = await AssertAsync();

            // Handle chained assertions with proper AND/OR logic
            foreach (var (chainType, assertion) in _chainedAssertions)
            {
                if (chainType == ChainType.And && !result.IsPassed)
                {
                    // Short circuit on AND failure
                    ThrowIfFailed(result);
                    return;
                }

                if (chainType == ChainType.Or && result.IsPassed)
                {
                    // Short circuit on OR success
                    return;
                }

                var chainResult = await assertion.AssertAsync();

                if (chainType == ChainType.And)
                {
                    // For AND, both must pass
                    if (!chainResult.IsPassed)
                    {
                        ThrowIfFailed(chainResult);
                        return;
                    }
                }
                else if (chainType == ChainType.Or)
                {
                    // For OR, at least one must pass
                    if (chainResult.IsPassed)
                    {
                        return;
                    }
                    // Keep the last failure for OR chains
                    result = chainResult;
                }
            }

            // If we get here, check the final result
            ThrowIfFailed(result);
        }
        catch (Exception ex) when (!(ex is AssertionException))
        {
            // Wrap unexpected exceptions
            throw new AssertionException($"Assertion failed with exception: {ex.Message}", ex);
        }
    }

    private void ThrowIfFailed(AssertionResult result)
    {
        if (!result.IsPassed)
        {
            var message = result.Message;
            if (!string.IsNullOrEmpty(_becauseReason))
            {
                message += $" because {_becauseReason}";
            }
            throw new AssertionException(message);
        }
    }

    /// <summary>
    /// Make it awaitable - THIS triggers execution
    /// </summary>
    public override TaskAwaiter GetAwaiter() => ExecuteAsync().GetAwaiter();

    /// <summary>
    /// Override this for assertion logic - called ONLY when awaited
    /// </summary>
    protected abstract Task<AssertionResult> AssertAsync();
}

/// <summary>
/// Non-generic base for common functionality
/// </summary>
public abstract class AssertionBase
{
    public abstract TaskAwaiter GetAwaiter();

    public abstract Task ExecuteAsync();
}