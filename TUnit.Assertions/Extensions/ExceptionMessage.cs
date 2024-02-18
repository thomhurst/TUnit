using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public class ExceptionMessage<TActual, TAnd, TOr> : Connector<TActual, TAnd, TOr> 
    where TActual : Exception
    where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
    where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
{
    protected internal AssertionBuilder<TActual> AssertionBuilder { get; }

    public ExceptionMessage(AssertionBuilder<TActual> assertionBuilder, ConnectorType connectorType,
        BaseAssertCondition<TActual, TAnd, TOr>? otherAssertCondition) : base(connectorType, otherAssertCondition)
    {
        AssertionBuilder = assertionBuilder.AppendExpression("Message");
    }

    public BaseAssertCondition<TActual, TAnd, TOr> EqualTo(string expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return EqualTo(expected, StringComparison.Ordinal, doNotPopulateThisValue);
    }

    public BaseAssertCondition<TActual, TAnd, TOr> EqualTo(string expected, StringComparison stringComparison, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("stringComparison")] string doNotPopulateThisValue2 = "")
    {
        return Wrap(new DelegateAssertCondition<TActual, string, TAnd, TOr>(AssertionBuilder.AppendCallerMethodWithMultipleExpressions([doNotPopulateThisValue1, doNotPopulateThisValue2]), expected, (actual, _, _, self) =>
            {
                ArgumentNullException.ThrowIfNull(actual);
                return string.Equals(actual.Message, expected, stringComparison);
            },
            (_, actual) =>
                $"Exception had a message of '{actual?.Message}' instead of '{expected}'")
        );
    }
}