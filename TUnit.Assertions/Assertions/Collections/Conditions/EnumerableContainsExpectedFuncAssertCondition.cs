namespace TUnit.Assertions.AssertConditions.Collections;

public class EnumerableContainsExpectedFuncAssertCondition<TActual, TInner>(
    Func<TInner, bool> matcher, string? matcherString)
    : BaseAssertCondition<TActual>
    where TActual : IEnumerable<TInner>
{
    private bool _wasFound;
    protected override string GetExpectation() => $"to contain an entry matching {matcherString ?? "null"}";
    
    protected override ValueTask<AssertionResult> GetResult(
        TActual? actualValue, Exception? exception,
        AssertionMetadata assertionMetadata
    )
    {
        if (actualValue is null)
        {
            return FailWithMessage($"{ActualExpression ?? typeof(TActual).Name} is null");
        }

        foreach (var inner in actualValue)
        {
            if (matcher(inner))
            {
                _wasFound = true;
                FoundItem = inner;
                break;
            }
        }
        
        return AssertionResult
            .FailIf(_wasFound is false, "there was no match found in the collection");
    }

    public TInner? FoundItem { get; private set; }
}