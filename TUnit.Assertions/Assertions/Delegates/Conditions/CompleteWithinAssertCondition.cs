using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.Exceptions;
using TUnit.Assertions.Helpers;

namespace TUnit.Assertions.Assertions.Delegates;

public class CompleteWithinAssertCondition<TActual>(TimeSpan timeSpan) : DelegateAssertCondition<TActual>
{
    internal protected override string GetExpectation()
        => $"to complete within {timeSpan.PrettyPrint()}";

    protected override ValueTask<AssertionResult> GetResult(
        TActual? actualValue, Exception? exception,
        AssertionMetadata assertionMetadata
    )
        => AssertionResult
            .FailIf(exception is not null && exception.GetType().IsAssignableTo(typeof(CompleteWithinException)),
                "it took too long to complete")
        .OrFailIf(exception is not null,
            $"a {exception!.GetType().Name} was thrown")
        .OrFailIf(assertionMetadata.Duration > timeSpan,
            $"it took {assertionMetadata.Duration.PrettyPrint()}"
        );

    public override TimeSpan? WaitFor => timeSpan;
}
