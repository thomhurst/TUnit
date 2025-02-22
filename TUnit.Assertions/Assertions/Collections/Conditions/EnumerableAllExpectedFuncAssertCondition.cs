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
        var unmatchedEntries = GetUnmatchedEntries(actualValue);
        return AssertionResult
            .FailIf(actualValue is null, $"{ActualExpression ?? typeof(TActual).Name} is null")
            .And(unmatchedEntries);
    }

    private AssertionResult GetUnmatchedEntries(TActual? actualValue)
    {
        if(actualValue == null) return AssertionResult.Passed;
        long i = 0;
        foreach (var item in actualValue)
        {
            if (!matcher(item))
            {
                return AssertionResult.Fail($"not all entries in the collection matched, first unmatched entry found at index {i}: \"{item}\"");
            }
            i++;
        }
        return AssertionResult.Passed; 
    }
    
}