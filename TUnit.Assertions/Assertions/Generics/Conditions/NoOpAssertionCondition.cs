using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.Assertions.Generics.Conditions;

public class NoOpAssertionCondition<TActual>(string previousExpectation) : BaseAssertCondition<TActual>
{
    protected override string GetExpectation() => previousExpectation;

    protected override ValueTask<AssertionResult> GetResult(
        TActual? actualValue, Exception? exception,
        AssertionMetadata assertionMetadata
    )
        => AssertionResult.Passed;
}