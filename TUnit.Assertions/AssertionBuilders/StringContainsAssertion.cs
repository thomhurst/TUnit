using System;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.AssertionBuilders;

public class StringContainsAssertion : AssertionBase<string?>
{
    private readonly string _substring;
    private StringComparison _stringComparison = StringComparison.Ordinal;
    private bool _trimming = false;
    private bool _ignoringWhitespace = false;

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

    public StringContainsAssertion WithTrimming()
    {
        _trimming = true;
        return this;
    }

    public StringContainsAssertion IgnoringWhitespace()
    {
        _ignoringWhitespace = true;
        return this;
    }

    protected override async Task<AssertionResult> AssertAsync()
    {
        var actual = await GetActualValueAsync();

        if (actual == null)
        {
            return AssertionResult.Fail($"Expected string to contain '{_substring}' but was null");
        }

        var processedActual = actual;
        var processedSubstring = _substring;

        if (_trimming)
        {
            processedActual = processedActual.Trim();
            processedSubstring = processedSubstring.Trim();
        }

        if (_ignoringWhitespace)
        {
            processedActual = System.Text.RegularExpressions.Regex.Replace(processedActual, @"\s+", "");
            processedSubstring = System.Text.RegularExpressions.Regex.Replace(processedSubstring, @"\s+", "");
        }

        if (processedActual.Contains(processedSubstring, _stringComparison))
        {
            return AssertionResult.Passed;
        }

        return AssertionResult.Fail($"Expected string to contain '{_substring}' but was '{actual}'");
    }
}