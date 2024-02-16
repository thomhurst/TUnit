using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.AssertConditions.Throws;

public class ExceptionWithMessage<TActual, TAnd, TOr> : Connector<TActual, TAnd, TOr>
    where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
    where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
{
    protected AssertionBuilder<TActual> AssertionBuilder { get; }
    
    public ExceptionWithMessage(AssertionBuilder<TActual> assertionBuilder, ConnectorType connectorType, BaseAssertCondition<TActual, TAnd, TOr>? otherAssertCondition) : base(connectorType, otherAssertCondition)
    {
        AssertionBuilder = assertionBuilder
            .AppendExpression("Message");
    }

    public BaseAssertCondition<TActual, TAnd, TOr> EqualTo(string expected, [CallerArgumentExpression("expected")] string expectedExpression = "")
    {
        return EqualTo(expected, StringComparison.Ordinal, expectedExpression);
    }

    public BaseAssertCondition<TActual, TAnd, TOr> EqualTo(string expected, StringComparison stringComparison, [CallerArgumentExpression("expected")] string expression1 = "", [CallerArgumentExpression("stringComparison")] string expression2 = "")
    {
        return Wrap(new ThrowsWithMessageEqualToAssertCondition<TActual, TAnd, TOr>(AssertionBuilder.AppendCallerMethodWithMultipleExpressions([expression1, expression2]), expected, stringComparison));
    }

    public BaseAssertCondition<TActual, TAnd, TOr> Containing(string expected, [CallerArgumentExpression("expected")] string expectedExpression = "")
    {
        return Containing(expected, StringComparison.Ordinal);
    }

    public BaseAssertCondition<TActual, TAnd, TOr> Containing(string expected, StringComparison stringComparison, [CallerArgumentExpression("expected")] string expression1 = "", [CallerArgumentExpression("stringComparison")] string expression2 = "")
    {
        return Wrap(new ThrowsWithMessageContainingAssertCondition<TActual, TAnd, TOr>(AssertionBuilder.AppendCallerMethodWithMultipleExpressions([expression1, expression2]), expected, stringComparison));
    }
}