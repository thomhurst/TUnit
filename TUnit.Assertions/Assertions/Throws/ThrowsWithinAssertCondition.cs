using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.Extensions;
using TUnit.Assertions.Helpers;

namespace TUnit.Assertions.Assertions.Throws;

public class ThrowsWithinAssertCondition<TActual, TExpectedException>(TimeSpan timeSpan) : DelegateAssertCondition<TActual, TExpectedException>
    where TExpectedException : Exception
{
    protected override string GetExpectation()
        => $"to throw {typeof(TExpectedException).Name.PrependAOrAn()} within {timeSpan.PrettyPrint()}";

    protected override Task<AssertionResult> GetResult(TActual? actualValue, Exception? exception,
        AssertionMetadata assertionMetadata)
        => AssertionResult
        .FailIf(exception is null,
            "none was thrown")
        .OrFailIf(exception?.GetType().IsAssignableTo(typeof(TExpectedException)) != true,
            $"{exception?.GetType().Name.PrependAOrAn()} was thrown"
        )
        .OrFailIf(assertionMetadata.Duration > timeSpan,
            $"it threw after {assertionMetadata.Duration.PrettyPrint()}");
}