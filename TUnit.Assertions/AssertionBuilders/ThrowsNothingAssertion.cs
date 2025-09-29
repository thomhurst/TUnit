using System;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.Exceptions;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// Assertion that checks no exception is thrown and returns the result
/// </summary>
public class ThrowsNothingAssertion<TResult> : AssertionBase<TResult>
{
    public ThrowsNothingAssertion(Func<Task<TResult>> actualValueProvider)
        : base(actualValueProvider)
    {
    }

    protected override Task<AssertionResult> AssertAsync()
    {
        // This assertion just validates that the value provider doesn't throw
        // The actual execution happens in the value provider passed to the constructor
        return Task.FromResult(AssertionResult.Passed);
    }

    // Implicit conversion to Task<TResult>
    public static implicit operator Task<TResult>(ThrowsNothingAssertion<TResult> assertion)
    {
        return assertion.GetValueAsync();
    }

    /// <summary>
    /// Gets the value after performing the assertion
    /// </summary>
    public async Task<TResult> GetValueAsync()
    {
        await ExecuteAsync();
        return await GetActualValueAsync();
    }

    /// <summary>
    /// Custom GetAwaiter that returns the value when awaited
    /// </summary>
    public new System.Runtime.CompilerServices.TaskAwaiter<TResult> GetAwaiter()
    {
        return GetValueAsync().GetAwaiter();
    }
}