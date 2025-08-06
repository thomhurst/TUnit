using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.Assertions.Delegates.Conditions;

public class DelegateNotNullAssertCondition<T> : BaseAssertCondition<T>
{
    internal protected override string GetExpectation()
        => "to not be null";

    protected override ValueTask<AssertionResult> GetResult(T? actualValue, Exception? exception, AssertionMetadata assertionMetadata)
    {
        // For delegates, a null reference exception during execution indicates the delegate was null
        if (exception is NullReferenceException or ArgumentNullException)
        {
            return new ValueTask<AssertionResult>(AssertionResult.FailIf(true, "it was"));
        }

        // If we have a value or no exception, the delegate was not null
        return new ValueTask<AssertionResult>(AssertionResult.Passed);
    }
}