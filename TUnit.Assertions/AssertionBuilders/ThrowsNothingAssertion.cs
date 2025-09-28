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
    public ThrowsNothingAssertion(Func<Task<object?>> actualValueProvider)
        : base(async () =>
        {
            var actual = await actualValueProvider();

            if (actual is Func<TResult> func)
            {
                try
                {
                    return func();
                }
                catch (Exception ex)
                {
                    throw new AssertionException($"Expected no exception but got {ex.GetType().Name}: {ex.Message}");
                }
            }
            else if (actual is Func<Task<TResult>> asyncFunc)
            {
                try
                {
                    return await asyncFunc();
                }
                catch (Exception ex)
                {
                    throw new AssertionException($"Expected no exception but got {ex.GetType().Name}: {ex.Message}");
                }
            }
            else if (actual is Action action)
            {
                try
                {
                    action();
                    return default(TResult)!;
                }
                catch (Exception ex)
                {
                    throw new AssertionException($"Expected no exception but got {ex.GetType().Name}: {ex.Message}");
                }
            }
            else if (actual is Func<Task> asyncAction)
            {
                try
                {
                    await asyncAction();
                    return default(TResult)!;
                }
                catch (Exception ex)
                {
                    throw new AssertionException($"Expected no exception but got {ex.GetType().Name}: {ex.Message}");
                }
            }

            throw new AssertionException($"Expected a delegate but got {actual?.GetType().Name ?? "null"}");
        })
    {
    }

    protected override async Task<AssertionResult> AssertAsync()
    {
        try
        {
            await GetActualValueAsync();
            return AssertionResult.Passed;
        }
        catch (AssertionException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return AssertionResult.Fail($"Unexpected error: {ex.Message}");
        }
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