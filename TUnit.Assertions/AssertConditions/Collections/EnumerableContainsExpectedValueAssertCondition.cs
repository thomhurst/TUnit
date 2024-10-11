namespace TUnit.Assertions.AssertConditions.Collections;

public class EnumerableContainsExpectedValueAssertCondition<TActual, TInner>(
    TInner expected,
    IEqualityComparer<TInner?>? equalityComparer)
    : ExpectedValueAssertCondition<TActual, TInner>(expected)
    where TActual : IEnumerable<TInner>
{
    protected override string GetExpectation() => $"to contain {expected}";

    protected override AssertionResult GetResult(TActual? actualValue, TInner? inner)
        => AssertionResult
            .FailIf(
                () => actualValue is null,
                $"{ActualExpression ?? typeof(TActual).Name} is null")
            .OrFailIf(
                () => !actualValue!.Contains(inner, equalityComparer),
                "it was not found in the collection");
}