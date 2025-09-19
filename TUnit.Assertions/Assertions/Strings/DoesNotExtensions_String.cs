#nullable disable

using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.String;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Attributes;
using TUnit.Engine;

namespace TUnit.Assertions.Extensions;

public static partial class DoesNotExtensions
{
    public static InvokableValueAssertionBuilder<string> DoesNotMatch(this IValueSource<string> valueSource, string regex, [CallerArgumentExpression(nameof(regex))] string expression = "")
    {
        return DoesNotMatch(valueSource, new Regex(regex), expression);
    }

    public static InvokableValueAssertionBuilder<string> DoesNotMatch(this IValueSource<string> valueSource, Regex regex, [CallerArgumentExpression(nameof(regex))] string expression = "")
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<string, Regex>(regex,
                (actual, _, _) =>
                {
                    Verify.ArgNotNull(actual);
                    return !regex.IsMatch(actual);
                },
                (actual, _, _) => $"The regex \"{regex}\" matches with \"{actual}\"",
                $"to not match with {expression}")
            , [expression]);
    }
}
