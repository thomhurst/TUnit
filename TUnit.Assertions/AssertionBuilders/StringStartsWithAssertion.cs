using System;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.AssertionBuilders;

public class StringStartsWithAssertion : AssertionBase<string?>
{
    private readonly string _prefix;
    private StringComparison _stringComparison = StringComparison.Ordinal;

    public StringStartsWithAssertion(Func<Task<string?>> actualValueProvider, string prefix)
        : base(actualValueProvider)
    {
        _prefix = prefix;
    }

    public StringStartsWithAssertion(Func<string?> actualValueProvider, string prefix)
        : base(actualValueProvider)
    {
        _prefix = prefix;
    }

    public StringStartsWithAssertion(string? actualValue, string prefix)
        : base(actualValue)
    {
        _prefix = prefix;
    }

    public StringStartsWithAssertion IgnoringCase()
    {
        _stringComparison = StringComparison.OrdinalIgnoreCase;
        return this;
    }

    public StringStartsWithAssertion WithComparison(StringComparison comparison)
    {
        _stringComparison = comparison;
        return this;
    }

    protected override async Task<AssertionResult> AssertAsync()
    {
        var actual = await GetActualValueAsync();

        if (actual == null)
        {
            return AssertionResult.Fail($"Expected string to start with '{_prefix}' but was null");
        }

        if (actual.StartsWith(_prefix, _stringComparison))
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Fail($"Expected string to start with '{_prefix}' but was '{actual}'");
    }
}