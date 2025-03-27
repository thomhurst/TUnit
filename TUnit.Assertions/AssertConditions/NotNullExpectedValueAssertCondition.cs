namespace TUnit.Assertions.AssertConditions;

public class NotNullExpectedValueAssertCondition<TActual> : ConvertToAssertCondition<TActual?, TActual>
{
    protected override string GetExpectation()
        => "to not be null";

    public override ValueTask<(AssertionResult, TActual?)> ConvertValue(TActual? value)
    {
        return new ValueTask<(AssertionResult, TActual?)>
        (
            (
                AssertionResult.FailIf(value is null, "it was"),
                value!
            )
        );
    }
}