using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertConditions.Throws;

public class ThrowsExactTypeOfDelegateAssertCondition<TActual, TExpectedException> : DelegateAssertCondition<TActual, TExpectedException>
    where TExpectedException : Exception
{
    protected override string GetExpectation()
        => $"to throw exactly {typeof(TExpectedException).Name.PrependAOrAn()}";

    protected override Task<AssertionResult> GetResult(TActual? actualValue, Exception? exception)
        => AssertionResult
        .FailIf(
            () => exception is null,
            "none was thrown")
        .OrFailIf(
            () => exception!.GetType() != typeof(TExpectedException),
            $"{exception?.GetType().Name.PrependAOrAn()} was thrown"
        );
}