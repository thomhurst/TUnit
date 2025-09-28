using System;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// Reference equality/inequality assertion
/// </summary>
public class ReferenceAssertion<TActual> : AssertionBase<TActual> where TActual : class
{
    private readonly TActual? _expected;
    private readonly bool _shouldBeSame;

    public ReferenceAssertion(Func<Task<TActual>> actualValueProvider, TActual? expected, bool shouldBeSame)
        : base(actualValueProvider)
    {
        _expected = expected;
        _shouldBeSame = shouldBeSame;
    }

    public ReferenceAssertion(Func<TActual> actualValueProvider, TActual? expected, bool shouldBeSame)
        : base(actualValueProvider)
    {
        _expected = expected;
        _shouldBeSame = shouldBeSame;
    }

    public ReferenceAssertion(TActual actualValue, TActual? expected, bool shouldBeSame)
        : base(actualValue)
    {
        _expected = expected;
        _shouldBeSame = shouldBeSame;
    }

    protected override async Task<AssertionResult> AssertAsync()
    {
        var actual = await GetActualValueAsync();
        bool areSame = ReferenceEquals(actual, _expected);

        if (areSame == _shouldBeSame)
        {
            return AssertionResult.Passed;
        }

        if (_shouldBeSame)
        {
            return AssertionResult.Fail($"Expected {actual} to be the same reference as {_expected}");
        }
        else
        {
            return AssertionResult.Fail($"Expected {actual} to not be the same reference as {_expected}");
        }
    }
}