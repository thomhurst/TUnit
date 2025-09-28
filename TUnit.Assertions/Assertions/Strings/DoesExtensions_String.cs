#nullable disable

using System;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using TUnit.Assertions.Assertions.Base;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.AssertionBuilders.Core;
using TUnit.Assertions.AssertionBuilders.Interfaces;
using TUnit.Assertions.Assertions.Strings.Conditions;
using TUnit.Assertions.Attributes;
using TUnit.Engine;

namespace TUnit.Assertions.Extensions;

public static partial class DoesExtensions
{
    public static StringContainsAssertion Contains(this IValueSource<string> valueSource, string expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = null)
    {
        return Contains(valueSource, expected, StringComparison.Ordinal, doNotPopulateThisValue);
    }

    public static StringContainsAssertion Contains(this IValueSource<string> valueSource, string expected, StringComparison stringComparison, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue1 = null, [CallerArgumentExpression(nameof(stringComparison))] string doNotPopulateThisValue2 = null)
    {
        // If the valueSource is already an assertion, pass its chain
        IAssertionChain chain = null;
        if (valueSource is Assertion<string> assertion)
        {
            chain = assertion.GetChain();
        }
        
        return new StringContainsAssertion(valueSource, expected, stringComparison, chain);
    }

    public static AssertionBuilder<string> Matches(this IValueSource<string> valueSource, string regex, [CallerArgumentExpression(nameof(regex))] string expression = "")
    {
        return Matches(valueSource, new Regex(regex), expression);
    }

    public static AssertionBuilder<string> Matches(this IValueSource<string> valueSource, Regex regex, [CallerArgumentExpression(nameof(regex))] string expression = "")
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<string, Regex>(regex,
            (actual, _, _) =>
            {
                Verify.ArgNotNull(actual);
                return regex.IsMatch(actual);
            },
            (actual, _, _) => $"The regex \"{regex}\" does not match with \"{actual}\"",
            $"match {expression}")
            , [expression]);
    }
}
