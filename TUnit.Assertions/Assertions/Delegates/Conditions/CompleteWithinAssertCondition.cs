using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.Helpers;

namespace TUnit.Assertions.Assertions.Delegates;

public class CompleteWithinAssertCondition<TActual>(TimeSpan timeSpan) : DelegateAssertCondition<TActual>
{
    protected override string GetExpectation()
        => $"to complete within {timeSpan.PrettyPrint()}";

    protected override Task<AssertionResult> GetResult(TActual? actualValue, Exception? exception,
        AssertionMetadata assertionMetadata)
        => AssertionResult
        .FailIf(exception is not null,
            $"a {exception!.GetType().Name} was thrown")
        .OrFailIf(assertionMetadata.Duration > timeSpan,
            $"it took {assertionMetadata.Duration.PrettyPrint()}"
        );
}