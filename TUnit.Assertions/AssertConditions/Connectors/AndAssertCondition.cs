namespace TUnit.Assertions.AssertConditions.Connectors;

internal class AndAssertCondition<TActual> : BaseAssertCondition<TActual>
{
    private readonly BaseAssertCondition _condition1;
    private readonly BaseAssertCondition _condition2;

    public AndAssertCondition(BaseAssertCondition condition1, BaseAssertCondition condition2)
    {
        Verify.ArgNotNull(condition1);
        Verify.ArgNotNull(condition2);
        
        _condition1 = condition1;
        _condition2 = condition2;
    }


    // This method is not used, as the GetExpectationWithReason is overwritten
    // and uses the expectation from the two conditions.
    protected override string GetExpectation() => "";

    internal override string GetExpectationWithReason()
        => $"{_condition1.GetExpectationWithReason()}{Environment.NewLine} and {_condition2.GetExpectationWithReason()}";
    
    protected override async Task<AssertionResult> GetResult(TActual? actualValue, Exception? exception)
    {
        return (await _condition1.Assert(actualValue, exception, ActualExpression))
            .And(await _condition2.Assert(actualValue, exception, ActualExpression));
    }

    internal override void SetBecauseReason(BecauseReason becauseReason)
        => _condition2.SetBecauseReason(becauseReason);

    internal override string GetBecauseReason()
        => _condition2.GetBecauseReason();
}