using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public class ExceptionMessage<TActual, TAnd, TOr> : Connector<TActual, TAnd, TOr> 
    where TActual : Exception
    where TAnd : IAnd<TActual, TAnd, TOr>
    where TOr : IOr<TActual, TAnd, TOr>
{
    protected internal AssertionBuilder<TActual, TAnd, TOr> AssertionBuilder { get; }

    public ExceptionMessage(AssertionBuilder<TActual, TAnd, TOr> assertionBuilder, ChainType chainType,
        BaseAssertCondition<TActual>? otherAssertCondition) : base(chainType, otherAssertCondition)
    {
        AssertionBuilder = assertionBuilder.AppendExpression("Message");
    }

    public BaseAssertCondition<TActual> EqualTo(string expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return EqualTo(expected, StringComparison.Ordinal, doNotPopulateThisValue);
    }

    public BaseAssertCondition<TActual> EqualTo(string expected, StringComparison stringComparison, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("stringComparison")] string doNotPopulateThisValue2 = "")
    {
        return Combine(new DelegateAssertCondition<TActual, string, TAnd, TOr>(AssertionBuilder.AppendCallerMethodWithMultipleExpressions([doNotPopulateThisValue1, doNotPopulateThisValue2]), expected, (actual, _, _, _) =>
            {
                ArgumentNullException.ThrowIfNull(actual);
                return string.Equals(actual.Message, expected, stringComparison);
            },
            (_, actual) =>
                $"Exception had a message of '{actual?.Message}' instead of '{expected}'")
        );
    }
}