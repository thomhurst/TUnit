using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.Assertions.Generics.Conditions;

public class NoOpWithMessageAssertionCondition<TActual>(string expectation) : BaseAssertCondition<TActual>
{
    protected override string GetExpectation()
        => expectation;

    protected override ValueTask<AssertionResult> GetResult(
        TActual? actualValue, Exception? exception,
        AssertionMetadata assertionMetadata
    )
        => AssertionResult.Passed;
}