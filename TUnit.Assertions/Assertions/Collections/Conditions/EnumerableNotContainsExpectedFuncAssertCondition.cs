namespace TUnit.Assertions.AssertConditions.Collections;

public class EnumerableNotContainsExpectedFuncAssertCondition<TActual, TInner>(
    Func<TInner, bool> matcher, string matcherString)
    : BaseAssertCondition<TActual>
    where TActual : IEnumerable<TInner>
{
    protected override string GetExpectation() => $"to contain no entry matching {matcherString}";
    
    protected override Task<AssertionResult> GetResult(TActual? actualValue, Exception? exception)
    {
        return AssertionResult
            .FailIf(actualValue is null,
                $"{ActualExpression ?? typeof(TActual).Name} is null")
            .OrFailIf(actualValue!.Any(matcher),
                "there was a match found in the collection");
    }
}