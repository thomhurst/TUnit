namespace TUnit.Assertions.AssertConditions.Collections;

public class EnumerableAllExpectedFuncAssertCondition<TActual, TInner>(
    Func<TInner, bool> matcher, string? matcherString)
    : BaseAssertCondition<TActual>
    where TActual : IEnumerable<TInner>
{
    protected override string GetExpectation() => $"to contain only entries matching {matcherString ?? "null"}";
    
    protected override ValueTask<AssertionResult> GetResult(
        TActual? actualValue, Exception? exception,
        AssertionMetadata assertionMetadata
    )
    {
        return AssertionResult
            .FailIf(actualValue is null,
                $"{ActualExpression ?? typeof(TActual).Name} is null")
            .OrFailIf(!actualValue!.All(matcher),
                //TODO: Add entry that failed to match
                $"not all entries in the collection matched");
    }
}