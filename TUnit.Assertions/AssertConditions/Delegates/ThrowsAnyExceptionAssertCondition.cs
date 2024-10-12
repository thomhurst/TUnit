using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.Delegates;

public class ThrowsAnyExceptionAssertCondition<TActual>
    : DelegateAssertCondition<TActual, Exception>
{
    protected override string GetExpectation()
        => "to throw an exception";

    protected override AssertionResult GetResult(TActual? actualValue, Exception? exception)
        => AssertionResult.FailIf(
            () => exception is null,
            "none was thrown");
}