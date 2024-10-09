using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertConditions.Throws;

public class ThrowsNothingExpectedValueAssertCondition<TActual> : DelegateAssertCondition<TActual, Exception>
{
    protected internal override string GetFailureMessage()
        => "to throw nothing";

    protected internal override AssertionResult Passes(TActual? actualValue, Exception? exception)
        => AssertionResult
        .FailIf(
			() => exception is not null,
            $"{exception?.GetType().Name.PrependAOrAn()} was thrown");
}