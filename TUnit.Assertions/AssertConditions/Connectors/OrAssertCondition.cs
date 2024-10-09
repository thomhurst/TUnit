namespace TUnit.Assertions.AssertConditions.Connectors;

internal class OrAssertCondition<TActual> : BaseAssertCondition<TActual>
{
    private readonly BaseAssertCondition<TActual> _condition1;
    private readonly BaseAssertCondition<TActual> _condition2;

    public OrAssertCondition(BaseAssertCondition<TActual> condition1, BaseAssertCondition<TActual> condition2)
    {
        ArgumentNullException.ThrowIfNull(condition1);
        ArgumentNullException.ThrowIfNull(condition2);
        
        _condition1 = condition1;
        _condition2 = condition2;
    }

    protected internal override string GetFailureMessage() => $"{_condition1.OverriddenMessage ?? _condition1.GetFullFailureMessage()}{Environment.NewLine} or{Environment.NewLine}{_condition2.OverriddenMessage ?? _condition2.GetFullFailureMessage()}";

    protected override AssertionResult Passes(TActual? actualValue, Exception? exception)
    {
		return _condition1.Assert(actualValue, exception, ActualExpression);
		//TODO VAB:
		// return _condition1.Assert(actualValue, exception, null).IsPassed || _condition2.Assert(actualValue, exception, null).IsPassed;
	}

	internal override void SetBecauseReason(BecauseReason becauseReason)
    {
        _condition1.SetBecauseReason(becauseReason);
        _condition2.SetBecauseReason(becauseReason);
    }
}