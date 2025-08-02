using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertConditions.Throws;

public class ThrowsNothingAssertCondition<TActual> : DelegateAssertCondition<TActual, Exception>
{
    internal protected override string GetExpectation()
        => "to throw nothing";

    protected override ValueTask<AssertionResult> GetResult(
        TActual? actualValue, Exception? exception,
        AssertionMetadata assertionMetadata
    )
        => AssertionResult
        .FailIf(exception is not null,
            $"{exception?.GetType().Name.PrependAOrAn()} was thrown:{Environment.NewLine}{exception?.Message}");
}
