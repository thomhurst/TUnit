namespace TUnit.Assertions.AssertConditions;

public class NotNullExpectedValueAssertCondition<TActual> : BaseAssertCondition<TActual>
{
    protected override string GetExpectation()
        => "to not be null";

    protected override Task<AssertionResult> GetResult(TActual? actualValue, Exception? exception)
        => AssertionResult
            .FailIf(
                () => actualValue is null,
                "it was");
}