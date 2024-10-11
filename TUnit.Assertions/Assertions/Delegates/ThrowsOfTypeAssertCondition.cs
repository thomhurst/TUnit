using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.Delegates;

public class ThrowsOfTypeAssertCondition<TActual, TExpectedException> : DelegateAssertCondition<TActual, Exception>
{
    protected override string GetExpectation()
        => $"to throw {typeof(TExpectedException).Name.PrependAOrAn()}";

    protected override AssertionResult GetResult(TActual? actualValue, Exception? exception)
        => AssertionResult
        .FailIf(
            () => exception is null,
            $"none was thrown")
        .OrFailIf(
            () => !exception!.GetType().IsAssignableTo(typeof(TExpectedException)),
            $"{exception?.GetType().Name.PrependAOrAn()} was thrown"
        );
}