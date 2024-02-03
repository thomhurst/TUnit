namespace TUnit.Assertions.AssertConditions.Operators;

public class Or<TActual>
{
    private readonly BaseAssertCondition<TActual> _otherAssertCondition;

    public Or(BaseAssertCondition<TActual> otherAssertCondition)
    {
        _otherAssertCondition = otherAssertCondition;
    }
    
    public Is<TActual> Is => new(_otherAssertCondition.AssertionBuilder, ConnectorType.Or, _otherAssertCondition);
    public Has<TActual> Has => new(_otherAssertCondition.AssertionBuilder, ConnectorType.Or, _otherAssertCondition);
    public Does<TActual> Does => new(_otherAssertCondition.AssertionBuilder, ConnectorType.Or, _otherAssertCondition);
    public Throws<TActual> Throws => new(_otherAssertCondition.AssertionBuilder, ConnectorType.Or, _otherAssertCondition);
}