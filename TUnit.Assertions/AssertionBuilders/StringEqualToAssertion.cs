using System;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.Helpers;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// Simplified string equality assertion with lazy evaluation
/// </summary>
public class StringEqualToAssertion : AssertionBase<string?>
{
    private readonly string? _expected;
    private StringComparison _stringComparison = StringComparison.Ordinal;
    private bool _trim = false;
    private bool _nullAndEmptyEqual = false;
    private bool _ignoreWhitespace = false;

    // Constructor takes lazy value provider and expected value
    public StringEqualToAssertion(Func<Task<string?>> actualValueProvider, string? expected)
        : base(actualValueProvider)
    {
        _expected = expected;
    }

    public StringEqualToAssertion(Func<string?> actualValueProvider, string? expected)
        : base(actualValueProvider)
    {
        _expected = expected;
    }

    public StringEqualToAssertion(string? actualValue, string? expected)
        : base(actualValue)
    {
        _expected = expected;
    }

    // Fluent configuration methods - NO EVALUATION happens here
    public StringEqualToAssertion WithStringComparison(StringComparison comparison)
    {
        _stringComparison = comparison;
        return this;
    }

    public StringEqualToAssertion WithTrimming()
    {
        _trim = true;
        return this;
    }

    public StringEqualToAssertion WithNullAndEmptyEquality()
    {
        _nullAndEmptyEqual = true;
        return this;
    }

    public StringEqualToAssertion IgnoringWhitespace()
    {
        _ignoreWhitespace = true;
        return this;
    }

    public StringEqualToAssertion IgnoringCase()
    {
        _stringComparison = _stringComparison switch
        {
            StringComparison.Ordinal => StringComparison.OrdinalIgnoreCase,
            StringComparison.CurrentCulture => StringComparison.CurrentCultureIgnoreCase,
            StringComparison.InvariantCulture => StringComparison.InvariantCultureIgnoreCase,
            _ => _stringComparison
        };
        return this;
    }

    /// <summary>
    /// The actual assertion logic - ONLY called when awaited
    /// </summary>
    protected override async Task<AssertionResult> AssertAsync()
    {
        // NOW we get the actual value (lazy evaluation)
        var actual = await GetActualValueAsync();
        var expected = _expected;

        // Handle null/empty equality option
        if (_nullAndEmptyEqual)
        {
            var actualIsNullOrEmpty = string.IsNullOrEmpty(actual);
            var expectedIsNullOrEmpty = string.IsNullOrEmpty(expected);

            if (actualIsNullOrEmpty && expectedIsNullOrEmpty)
            {
                return AssertionResult.Passed;
            }
        }

        // Apply transformations
        if (_trim)
        {
            actual = actual?.Trim();
            expected = expected?.Trim();
        }

        if (_ignoreWhitespace)
        {
            actual = actual?.Replace(" ", "").Replace("\t", "").Replace("\r", "").Replace("\n", "");
            expected = expected?.Replace(" ", "").Replace("\t", "").Replace("\r", "").Replace("\n", "");
        }

        // Perform the comparison
        if (string.Equals(actual, expected, _stringComparison))
        {
            return AssertionResult.Passed;
        }

        // Generate failure message
        var message = GenerateFailureMessage(actual, expected);
        return AssertionResult.Fail(message);
    }

    private string GenerateFailureMessage(string? actual, string? expected)
    {
        if (actual == null && expected != null)
        {
            return $"Expected string to be \"{expected}\" but was null";
        }

        if (actual != null && expected == null)
        {
            return $"Expected string to be null but was \"{actual}\"";
        }

        // Try to find where they differ
        if (actual != null && expected != null)
        {
            var difference = new StringDifference(actual, expected);
            return $"Expected string to be \"{expected}\" but was \"{actual}\"\n{difference}";
        }

        return $"Expected \"{expected}\" but was \"{actual}\"";
    }
}

// Extension method to make it easy to use
public static class StringAssertionExtensions
{
    public static StringEqualToAssertion IsEqualToSimplified(this string? actual, string? expected)
    {
        return new StringEqualToAssertion(actual, expected);
    }

    public static StringEqualToAssertion IsEqualToSimplified(this Func<string?> actualProvider, string? expected)
    {
        return new StringEqualToAssertion(actualProvider, expected);
    }

    public static StringEqualToAssertion IsEqualToSimplified(this Task<string?> actualTask, string? expected)
    {
        return new StringEqualToAssertion(() => actualTask, expected);
    }
}