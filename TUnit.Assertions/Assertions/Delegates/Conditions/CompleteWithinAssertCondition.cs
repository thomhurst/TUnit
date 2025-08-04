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
    {
        if (exception is not null && exception.GetType().IsAssignableTo(typeof(CompleteWithinException)))
        {
            return AssertionResult.Fail("it took too long to complete");
        }

        if (exception is not null)
        {
            return AssertionResult.Fail($"a {exception.GetType().Name} was thrown");
        }

        if (assertionMetadata.Duration > timeSpan)
        {
            return AssertionResult.Fail($"it took {assertionMetadata.Duration.PrettyPrint()}");
        }

        return AssertionResult.Passed;
    }

    public override TimeSpan? WaitFor => timeSpan;
}
