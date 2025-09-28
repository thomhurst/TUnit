#nullable disable

using System;
using System.Runtime.CompilerServices;
using TUnit.Assertions.Assertions.Base;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.String;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.AssertionBuilders.Core;
using TUnit.Assertions.AssertionBuilders.Interfaces;
using TUnit.Assertions.Attributes;

namespace TUnit.Assertions.Extensions;

public static class StringIsExtensions
{
    public static StringEqualToAssertion IsEqualTo(this IValueSource<string> valueSource, string expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue1 = null)
    {
        return IsEqualTo(valueSource, expected, StringComparison.Ordinal, doNotPopulateThisValue1, null);
    }

    public static StringEqualToAssertion IsEqualTo(this IValueSource<string> valueSource, string expected, StringComparison stringComparison, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue1 = null, [CallerArgumentExpression(nameof(stringComparison))] string doNotPopulateThisValue2 = null)
    {
        // If the valueSource is already an assertion, pass its chain
        IAssertionChain chain = null;
        if (valueSource is Assertion<string> assertion)
        {
            chain = assertion.GetChain();
        }
        
        return new StringEqualToAssertion(valueSource, expected, stringComparison, chain);
    }

    public static AssertionBuilder<string> IsEmpty(this IValueSource<string> valueSource)
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<string, int>(0,
            (value, _, self) =>
            {
                if (value is null)
                {
                    self.FailWithMessage("Actual string is null");
                    return false;
                }

                return value == string.Empty;
            },
            (s, _, _) => $"'{s}' was not empty with {s.Length} characters",
            $"to be empty")
            , []);
    }

    public static AssertionBuilder<string> IsNullOrEmpty(this IValueSource<string> valueSource)
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<string, int>(0,
            (value, _, _) => string.IsNullOrEmpty(value),
            (s, _, _) => $"'{s}' is not null or empty",
            $"to be null or empty")
            , []);
    }

    public static AssertionBuilder<string> IsNullOrWhitespace(this IValueSource<string> valueSource)
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<string, int>(0,
            (value, _, _) => string.IsNullOrWhiteSpace(value),
            (s, _, _) => $"'{s}' is not null or whitespace",
            $"to be null or whitespace")
            , []);
    }
}
