namespace TUnit.Assertions.AssertConditions.Throws;

public class ThrowsAnyExceptionAssertCondition<TActual>
    : DelegateAssertCondition<TActual, Exception>
{
    protected override string GetExpectation()
        => "to throw an exception";

    protected override Task<AssertionResult> GetResult(TActual? actualValue, Exception? exception,
        AssertionMetadata assertionMetadata)
        => AssertionResult.FailIf(exception is null,
            "none was thrown");
}