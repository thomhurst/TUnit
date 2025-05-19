namespace TUnit.Assertions.AssertConditions;

public class NotNullExpectedValueAssertCondition<TActual> : ConvertToAssertCondition<TActual?, TActual> where TActual : class?
{
    internal protected override string GetExpectation()
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

public class NotNullStructExpectedValueAssertCondition<TActual> : ConvertToAssertCondition<TActual?, TActual> where TActual : struct
{
    internal protected override string GetExpectation()
        => "to not be null";

    public override ValueTask<(AssertionResult, TActual)> ConvertValue(TActual? value)
    {
        return new ValueTask<(AssertionResult, TActual)>
        (
            (
                AssertionResult.FailIf(!value.HasValue, "it was"),
                value ?? default(TActual)
            )
        );
    }
}