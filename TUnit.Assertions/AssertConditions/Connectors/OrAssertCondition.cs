namespace TUnit.Assertions.AssertConditions.Connectors;

internal class OrAssertCondition : BaseAssertCondition
{
    private readonly BaseAssertCondition _condition1;
    private readonly BaseAssertCondition _condition2;

    public OrAssertCondition(BaseAssertCondition condition1, BaseAssertCondition condition2)
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
        => $"{_condition1.GetExpectationWithReason()}{Environment.NewLine} or {_condition2.GetExpectationWithReason()}";

    internal sealed override async Task<AssertionResult> GetAssertionResult(object? actualValue, Exception? exception, AssertionMetadata assertionMetadata, string? actualExpression)
    {
        return (await _condition1.GetAssertionResult(actualValue, exception, assertionMetadata, actualExpression))
            .Or(await _condition2.GetAssertionResult(actualValue, exception, assertionMetadata, actualExpression));
    }

    internal override void SetBecauseReason(BecauseReason becauseReason)
        => _condition2.SetBecauseReason(becauseReason);

    internal override string GetBecauseReason()
        => _condition2.GetBecauseReason();
}