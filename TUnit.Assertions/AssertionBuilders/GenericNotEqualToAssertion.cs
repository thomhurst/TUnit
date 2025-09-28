using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
///  generic not-equal assertion with lazy evaluation
/// </summary>
public class GenericNotEqualToAssertion<TActual> : AssertionBase<TActual>
{
    private readonly TActual _expected;
    private IEqualityComparer<TActual>? _comparer;

    public GenericNotEqualToAssertion(Func<Task<TActual>> actualValueProvider, TActual expected)
        : base(actualValueProvider)
    {
        _expected = expected;
    }

    public GenericNotEqualToAssertion(Func<TActual> actualValueProvider, TActual expected)
        : base(actualValueProvider)
    {
        _expected = expected;
    }

    public GenericNotEqualToAssertion(TActual actualValue, TActual expected)
        : base(actualValue)
    {
        _expected = expected;
    }

    public GenericNotEqualToAssertion<TActual> WithComparer(IEqualityComparer<TActual> comparer)
    {
        _comparer = comparer;
        return this;
    }

    protected override async Task<AssertionResult> AssertAsync()
    {
        var actual = await GetActualValueAsync();

        // Use custom comparer if provided
        var comparer = _comparer ?? EqualityComparer<TActual>.Default;

        if (!comparer.Equals(actual, _expected))
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Fail($"Expected value to not be {_expected} but it was");
    }
}