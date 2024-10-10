using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertConditions.Throws;

public class ThrowsSubClassOfExpectedValueAssertCondition<TActual, TExpectedException> : DelegateAssertCondition<TActual, Exception>
{
    protected override string GetExpectation()
        => $"to throw {typeof(TExpectedException).Name.PrependAOrAn()}";

    protected override AssertionResult GetResult(TActual? actualValue, Exception? exception)
        => AssertionResult
        .FailIf(
            () => exception is null,
            $"none was thrown")
        .OrFailIf(
            () => !exception.GetType().IsSubclassOf(typeof(TExpectedException)),
            $"{exception?.GetType().Name.PrependAOrAn()} was thrown"
        );
}