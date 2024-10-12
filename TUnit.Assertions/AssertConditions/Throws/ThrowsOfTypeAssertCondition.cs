using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertConditions.Throws;

public class ThrowsOfTypeAssertCondition<TActual, TExpectedException> : DelegateAssertCondition<TActual, Exception>
{
    protected override string GetExpectation()
        => $"to throw {typeof(TExpectedException).Name.PrependAOrAn()}";

    protected override Task<AssertionResult> GetResult(TActual? actualValue, Exception? exception)
        => AssertionResult
        .FailIf(
            () => exception is null,
            "none was thrown")
        .OrFailIf(
            () => !exception!.GetType().IsAssignableTo(typeof(TExpectedException)),
            $"{exception?.GetType().Name.PrependAOrAn()} was thrown"
        );
}