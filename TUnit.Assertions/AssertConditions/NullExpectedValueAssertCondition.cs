namespace TUnit.Assertions.AssertConditions;

public class NullExpectedValueAssertCondition<TActual> : BaseAssertCondition<TActual>
{
    protected override string GetExpectation()
        => "to be null";

    protected override Task<AssertionResult> GetResult(TActual? actualValue, Exception? exception,
        AssertionMetadata assertionMetadata)
        => AssertionResult
            .FailIf(actualValue is not null,
                $"found {actualValue}");
}