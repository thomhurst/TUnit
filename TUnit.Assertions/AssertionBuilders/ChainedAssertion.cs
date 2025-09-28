using System;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// Represents a chained assertion that combines multiple assertions
/// </summary>
public class ChainedAssertion<T> : AssertionBase<T>
{
    private readonly AssertionBase<T> _baseAssertion;

    public ChainedAssertion(AssertionBase<T> baseAssertion)
        : base(() => Task.FromResult(default(T)!)) // We don't actually need the value for chaining
    {
        _baseAssertion = baseAssertion;
    }

    protected override async Task<AssertionResult> AssertAsync()
    {
        // Execute the base assertion which should handle all chained assertions
        await _baseAssertion.ExecuteAsync();
        return AssertionResult.Passed;
    }
}