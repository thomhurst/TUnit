using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.Assertions.Generics.Conditions;

public class NoOpAssertionCondition<TActual> : BaseAssertCondition<TActual>
{
    protected override string GetExpectation() => string.Empty;

    protected override ValueTask<AssertionResult> GetResult(
        TActual? actualValue, Exception? exception,
        AssertionMetadata assertionMetadata
    )
        => AssertionResult.Passed;
}