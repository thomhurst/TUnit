using TUnit.Assertions.AssertConditions.Operators;

namespace TUnit.Assertions.AssertConditions.Throws;

public class WithMessage<TActual, TAnd, TOr> : Connector<TActual, TAnd, TOr>
    where TAnd : And<TActual, TAnd, TOr>, IAnd<TAnd, TActual, TAnd, TOr>
    where TOr : Or<TActual, TAnd, TOr>, IOr<TOr, TActual, TAnd, TOr>
{
    protected AssertionBuilder<TActual> AssertionBuilder { get; }
    
    public WithMessage(AssertionBuilder<TActual> assertionBuilder, ConnectorType connectorType, BaseAssertCondition<TActual, TAnd, TOr>? otherAssertCondition) : base(connectorType, otherAssertCondition)
    {
        AssertionBuilder = assertionBuilder;
    }

    public BaseAssertCondition<TActual, TAnd, TOr> EqualTo(string expected)
    {
        return EqualTo(expected, StringComparison.Ordinal);
    }

    public BaseAssertCondition<TActual, TAnd, TOr> EqualTo(string expected, StringComparison stringComparison)
    {
        return Wrap(new ThrowsWithMessageEqualToAssertCondition<TActual, TAnd, TOr>(AssertionBuilder, expected, stringComparison));
    }

    public BaseAssertCondition<TActual, TAnd, TOr> Containing(string expected)
    {
        return Containing(expected, StringComparison.Ordinal);
    }

    public BaseAssertCondition<TActual, TAnd, TOr> Containing(string expected, StringComparison stringComparison)
    {
        return Wrap(new ThrowsWithMessageContainingAssertCondition<TActual, TAnd, TOr>(AssertionBuilder, expected, stringComparison));
    }
}