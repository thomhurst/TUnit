using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Throws;

public class ExceptionWithMessage<TActual, TAnd, TOr> : Connector<TActual, TAnd, TOr>
    where TAnd : IAnd<TActual, TAnd, TOr>
    where TOr : IOr<TActual, TAnd, TOr>
{
    private readonly Func<Exception?, Exception?> _exceptionSelector;
    protected AssertionBuilder<TActual, TAnd, TOr> AssertionBuilder { get; }
    
    public ExceptionWithMessage(AssertionBuilder<TActual, TAnd, TOr> assertionBuilder, ChainType chainType,
        BaseAssertCondition<TActual>? otherAssertCondition, Func<Exception?, Exception?> exceptionSelector) : base(chainType, otherAssertCondition)
    {
        _exceptionSelector = exceptionSelector;
        AssertionBuilder = assertionBuilder
            .AppendExpression("Message");
    }

    public BaseAssertCondition<TActual> EqualTo(string expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return EqualTo(expected, StringComparison.Ordinal, doNotPopulateThisValue);
    }

    public BaseAssertCondition<TActual> EqualTo(string expected, StringComparison stringComparison, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("stringComparison")] string doNotPopulateThisValue2 = "")
    {
        return Combine(new ThrowsWithMessageEqualToAssertCondition<TActual, TAnd, TOr>(AssertionBuilder.AppendCallerMethodWithMultipleExpressions([doNotPopulateThisValue1, doNotPopulateThisValue2]), expected, stringComparison, _exceptionSelector));
    }

    public BaseAssertCondition<TActual> Containing(string expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return Containing(expected, StringComparison.Ordinal, doNotPopulateThisValue);
    }

    public BaseAssertCondition<TActual> Containing(string expected, StringComparison stringComparison, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("stringComparison")] string doNotPopulateThisValue2 = "")
    {
        return Combine(new ThrowsWithMessageContainingAssertCondition<TActual, TAnd, TOr>(AssertionBuilder.AppendCallerMethodWithMultipleExpressions([doNotPopulateThisValue1, doNotPopulateThisValue2]), expected, stringComparison, _exceptionSelector));
    }
}