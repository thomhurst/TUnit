﻿namespace TUnit.Assertions.AssertConditions.Connectors;

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

    protected override string GetExpectation()
	    => throw new NotSupportedException();

    internal override string GetExpectationWithReason()
	    => $"{_condition1.GetExpectationWithReason()}{Environment.NewLine} and{Environment.NewLine}{_condition2.GetExpectationWithReason()}";
    
    protected internal override AssertionResult Passes(TActual? actualValue, Exception? exception)
    {
        return _condition1.Assert(actualValue, exception, ActualExpression)
	        .And(_condition2.Assert(actualValue, exception, ActualExpression));
	}

    internal override void SetBecauseReason(BecauseReason becauseReason)
	    => _condition2.SetBecauseReason(becauseReason);

    internal override string GetBecauseReason()
	    => _condition2.GetBecauseReason();
}