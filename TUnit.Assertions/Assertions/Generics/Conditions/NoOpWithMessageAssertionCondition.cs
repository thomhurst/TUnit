namespace TUnit.Assertions.AssertConditions.Generic;

public class NoOpWithMessageAssertionCondition<TActual>(string expectation) : BaseAssertCondition<TActual>
{
    protected override string GetExpectation()
        => expectation;

    protected override Task<AssertionResult> GetResult(TActual? actualValue, Exception? exception)
        => AssertionResult.Passed;
}