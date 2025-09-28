using System;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.Exceptions;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// Assertion that checks the type and allows chaining
/// </summary>
public class TypeOfAssertion<TExpected> : AssertionBase<TExpected>
    where TExpected : class
{
    public TypeOfAssertion(Func<Task<object>> actualValueProvider)
        : base(async () =>
        {
            var actual = await actualValueProvider();
            if (actual == null)
            {
                throw new AssertionException($"Expected type {typeof(TExpected).Name} but was null");
            }
            if (actual is TExpected expected)
            {
                return expected;
            }
            throw new AssertionException($"Expected type {typeof(TExpected).Name} but was {actual.GetType().Name}");
        })
    {
    }

    public TypeOfAssertion(Func<object> actualValueProvider)
        : base(() =>
        {
            var actual = actualValueProvider();
            if (actual == null)
            {
                throw new AssertionException($"Expected type {typeof(TExpected).Name} but was null");
            }
            if (actual is TExpected expected)
            {
                return Task.FromResult(expected);
            }
            throw new AssertionException($"Expected type {typeof(TExpected).Name} but was {actual.GetType().Name}");
        })
    {
    }

    // Allow And chaining by returning an AssertionBuilder
    public new AssertionBuilder<TExpected> And => new AssertionBuilder<TExpected>(GetActualValueAsync);

    protected override async Task<AssertionResult> AssertAsync()
    {
        try
        {
            await GetActualValueAsync();
            return AssertionResult.Passed;
        }
        catch (AssertionException)
        {
            // Re-throw assertion exceptions
            throw;
        }
        catch (Exception ex)
        {
            return AssertionResult.Fail($"Type assertion failed: {ex.Message}");
        }
    }

    // Implicit conversion to Task<TExpected>
    public static implicit operator Task<TExpected>(TypeOfAssertion<TExpected> assertion)
    {
        return assertion.GetValueAsync();
    }

    /// <summary>
    /// Gets the value after performing the type assertion
    /// </summary>
    public async Task<TExpected> GetValueAsync()
    {
        await ExecuteAsync();
        return await GetActualValueAsync();
    }

    /// <summary>
    /// Custom GetAwaiter that returns the value when awaited
    /// </summary>
    public new System.Runtime.CompilerServices.TaskAwaiter<TExpected> GetAwaiter()
    {
        return GetValueAsync().GetAwaiter();
    }
}