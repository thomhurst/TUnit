using System;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
///  null assertion with lazy evaluation
/// </summary>
public class NullAssertion<TActual> : AssertionBase<TActual>
{
    private readonly bool _shouldBeNull;
    private TActual? _actualValue;

    public NullAssertion(Func<Task<TActual>> actualValueProvider, bool shouldBeNull)
        : base(actualValueProvider)
    {
        _shouldBeNull = shouldBeNull;
    }

    public NullAssertion(Func<TActual> actualValueProvider, bool shouldBeNull)
        : base(actualValueProvider)
    {
        _shouldBeNull = shouldBeNull;
    }

    public NullAssertion(TActual actualValue, bool shouldBeNull)
        : base(actualValue)
    {
        _shouldBeNull = shouldBeNull;
    }

    protected override async Task<AssertionResult> AssertAsync()
    {
        var actual = await GetActualValueAsync();
        _actualValue = actual;

        var isNull = actual == null;

        if (isNull == _shouldBeNull)
        {
            return AssertionResult.Passed;
        }

        if (_shouldBeNull)
        {
            return AssertionResult.Fail($"Expected null but was {actual}");
        }
        else
        {
            return AssertionResult.Fail("Expected non-null value but was null");
        }
    }

    // Custom GetAwaiter for IsNotNull that returns the non-null value
    public new System.Runtime.CompilerServices.TaskAwaiter<TActual> GetAwaiter()
    {
        if (!_shouldBeNull)
        {
            return GetNonNullValueAsync().GetAwaiter();
        }
        // For IsNull assertion, we still need to return TActual (which will be null)
        return GetNullValueAsync().GetAwaiter();
    }

    private async Task<TActual> GetNonNullValueAsync()
    {
        await ExecuteAsync();
        if (_actualValue == null)
        {
            throw new InvalidOperationException("Value was null");
        }
        return _actualValue;
    }

    private async Task<TActual> GetNullValueAsync()
    {
        await ExecuteAsync();
        return _actualValue!; // Will be null, but we return it as TActual
    }
}