using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertConditions.Throws;

public class ThrowsNothingAssertCondition<TActual> : DelegateAssertCondition<TActual, Exception>
{
    protected override string GetExpectation()
        => "to throw nothing";

    protected override Task<AssertionResult> GetResult(TActual? actualValue, Exception? exception)
        => AssertionResult
        .FailIf(
            () => exception is not null,
            $"{exception?.GetType().Name.PrependAOrAn()} was thrown:{Environment.NewLine}{exception?.Message}");
}