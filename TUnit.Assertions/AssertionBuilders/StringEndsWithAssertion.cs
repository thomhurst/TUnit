using System;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.AssertionBuilders;

public class StringEndsWithAssertion : AssertionBase<string?>
{
    private readonly string _suffix;
    private StringComparison _stringComparison = StringComparison.Ordinal;

    public StringEndsWithAssertion(Func<Task<string?>> actualValueProvider, string suffix)
        : base(actualValueProvider)
    {
        _suffix = suffix;
    }

    public StringEndsWithAssertion(Func<string?> actualValueProvider, string suffix)
        : base(actualValueProvider)
    {
        _suffix = suffix;
    }

    public StringEndsWithAssertion(string? actualValue, string suffix)
        : base(actualValue)
    {
        _suffix = suffix;
    }

    public StringEndsWithAssertion IgnoringCase()
    {
        _stringComparison = StringComparison.OrdinalIgnoreCase;
        return this;
    }

    public StringEndsWithAssertion WithComparison(StringComparison comparison)
    {
        _stringComparison = comparison;
        return this;
    }

    protected override async Task<AssertionResult> AssertAsync()
    {
        var actual = await GetActualValueAsync();

        if (actual == null)
        {
            return AssertionResult.Fail($"Expected string to end with '{_suffix}' but was null");
        }

        if (actual.EndsWith(_suffix, _stringComparison))
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Fail($"Expected string to end with '{_suffix}' but was '{actual}'");
    }
}