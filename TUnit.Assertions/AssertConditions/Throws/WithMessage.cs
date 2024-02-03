namespace TUnit.Assertions.AssertConditions.Throws;

public class WithMessage<TActual> : Connector<TActual>
{
    protected AssertionBuilder<TActual> AssertionBuilder { get; }
    
    public WithMessage(AssertionBuilder<TActual> assertionBuilder, ConnectorType connectorType, BaseAssertCondition<TActual>? otherAssertCondition) : base(connectorType, otherAssertCondition)
    {
        AssertionBuilder = assertionBuilder;
    }

    public BaseAssertCondition<TActual> EqualTo(string expected)
    {
        return EqualTo(expected, StringComparison.Ordinal);
    }

    public BaseAssertCondition<TActual> EqualTo(string expected, StringComparison stringComparison)
    {
        return Wrap(new ThrowsWithMessageEqualToAssertCondition<TActual>(AssertionBuilder, expected, stringComparison));
    }

    public BaseAssertCondition<TActual> Containing(string expected)
    {
        return Containing(expected, StringComparison.Ordinal);
    }

    public BaseAssertCondition<TActual> Containing(string expected, StringComparison stringComparison)
    {
        return Wrap(new ThrowsWithMessageContainingAssertCondition<TActual>(AssertionBuilder, expected, stringComparison));
    }
}