using System;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
///  boolean assertion with lazy evaluation
/// </summary>
public class BooleanAssertion : AssertionBase<bool>
{
    private readonly bool _expectedValue;

    public BooleanAssertion(Func<Task<bool>> actualValueProvider, bool expectedValue)
        : base(actualValueProvider)
    {
        _expectedValue = expectedValue;
    }

    public BooleanAssertion(Func<bool> actualValueProvider, bool expectedValue)
        : base(actualValueProvider)
    {
        _expectedValue = expectedValue;
    }

    public BooleanAssertion(bool actualValue, bool expectedValue)
        : base(actualValue)
    {
        _expectedValue = expectedValue;
    }

    protected override async Task<AssertionResult> AssertAsync()
    {
        var actual = await GetActualValueAsync();

        if (actual == _expectedValue)
        {
            return AssertionResult.Passed;
        }

        if (_expectedValue)
        {
            return AssertionResult.Fail("Expected true but was false");
        }
        else
        {
            return AssertionResult.Fail("Expected false but was true");
        }
    }
}