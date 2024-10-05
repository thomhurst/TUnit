namespace TUnit.Assertions.AssertConditions.Connectors;

internal class AndAssertCondition<TActual> : BaseAssertCondition<TActual>
{
    private readonly BaseAssertCondition<TActual> _condition1;
    private readonly BaseAssertCondition<TActual> _condition2;

    public AndAssertCondition(BaseAssertCondition<TActual> condition1, BaseAssertCondition<TActual> condition2)
    {
        ArgumentNullException.ThrowIfNull(condition1);
        ArgumentNullException.ThrowIfNull(condition2);
        
        _condition1 = condition1;
        _condition2 = condition2;
    }

    protected internal override string GetFailureMessage()
    {
        var messages = new List<string>(2);
            
        if (!_condition1.Assert(ActualValue, Exception, ActualExpression))
        {
            messages.Add(_condition1.OverriddenMessage ?? _condition1.GetFailureMessage());
        }
            
        if (!_condition2.Assert(ActualValue, Exception, ActualExpression))
        {
            messages.Add(_condition2.OverriddenMessage ?? _condition2.GetFailureMessage());
        }

        return string.Join($"{Environment.NewLine} and{Environment.NewLine}", messages);
    }
    
    protected override bool Passes(TActual? actualValue, Exception? exception)
    {
        return _condition1.Assert(actualValue, exception, ActualExpression) && _condition2.Assert(actualValue, exception, ActualExpression);
    }

    internal override void SetBecauseReason(BecauseReason becauseReason)
    {
        _condition1.SetBecauseReason(becauseReason);
        _condition2.SetBecauseReason(becauseReason);
    }
}