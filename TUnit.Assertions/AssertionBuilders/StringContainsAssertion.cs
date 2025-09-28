using System;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.AssertionBuilders;

public class StringContainsAssertion : AssertionBase<string?>
{
    private readonly string _substring;
    private StringComparison _stringComparison = StringComparison.Ordinal;

    public StringContainsAssertion(Func<Task<string?>> actualValueProvider, string substring)
        : base(actualValueProvider)
    {
        _substring = substring;
    }

    public StringContainsAssertion(Func<string?> actualValueProvider, string substring)
        : base(actualValueProvider)
    {
        _substring = substring;
    }

    public StringContainsAssertion(string? actualValue, string substring)
        : base(actualValue)
    {
        _substring = substring;
    }

    public StringContainsAssertion IgnoringCase()
    {
        _stringComparison = StringComparison.OrdinalIgnoreCase;
        return this;
    }

    public StringContainsAssertion WithComparison(StringComparison comparison)
    {
        _stringComparison = comparison;
        return this;
    }

    protected override async Task<AssertionResult> AssertAsync()
    {
        var actual = await GetActualValueAsync();

        if (actual == null)
        {
            return AssertionResult.Fail($"Expected string to contain '{_substring}' but was null");
        }

        if (actual.Contains(_substring, _stringComparison))
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Fail($"Expected string to contain '{_substring}' but was '{actual}'");
    }
}