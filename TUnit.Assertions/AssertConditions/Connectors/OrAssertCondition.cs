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

    // This method is not used, as the GetExpectationWithReason is overwritten
    // and uses the expectation from the two conditions.
    protected override string GetExpectation() => "";

    internal override string GetExpectationWithReason()
        => $"{_condition1.GetExpectationWithReason()}{Environment.NewLine} or {_condition2.GetExpectationWithReason()}";

    protected override async Task<AssertionResult> GetResult(TActual? actualValue, Exception? exception)
    {
        return (await _condition1.Assert(actualValue, exception, ActualExpression))
            .Or(await _condition2.Assert(actualValue, exception, ActualExpression));
    }

    internal override void SetBecauseReason(BecauseReason becauseReason)
        => _condition2.SetBecauseReason(becauseReason);

    internal override string GetBecauseReason()
        => _condition2.GetBecauseReason();
}