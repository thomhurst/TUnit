namespace TUnit.Assertions.AssertConditions.Collections;

public class EnumerableContainsExpectedFuncAssertCondition<TActual, TInner>(
    Func<TInner, bool> matcher, string matcherString)
    : BaseAssertCondition<TActual>
    where TActual : IEnumerable<TInner>
{
    protected override string GetExpectation() => $"to contain an entry matching {matcherString}";
    
    protected override Task<AssertionResult> GetResult(TActual? actualValue, Exception? exception)
    {
        return AssertionResult
            .FailIf(actualValue is null,
                $"{ActualExpression ?? typeof(TActual).Name} is null")
            .OrFailIf(!actualValue!.Any(matcher),
                "there was no match found in the collection");
    }
}