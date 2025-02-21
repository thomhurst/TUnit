using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertConditions.Throws;

public class ThrowsOfTypeAssertCondition<TActual, TExpectedException> : DelegateAssertCondition<TActual, Exception>
{
    protected override string GetExpectation()
        => $"to throw {typeof(TExpectedException).Name.PrependAOrAn()}";

    protected override ValueTask<AssertionResult> GetResult(
        TActual? actualValue, Exception? exception,
        AssertionMetadata assertionMetadata
    )
        => AssertionResult
        .FailIf(exception is null, "none was thrown")
        .OrFailIf(!typeof(TExpectedException).IsAssignableFrom(exception?.GetType()),
            $"{exception?.GetType().Name.PrependAOrAn()} was thrown"
        );
}