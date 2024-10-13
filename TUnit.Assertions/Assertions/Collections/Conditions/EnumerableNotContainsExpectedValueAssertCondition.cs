namespace TUnit.Assertions.AssertConditions.Collections;

public class EnumerableNotContainsExpectedValueAssertCondition<TActual, TInner>(
    TInner expected,
    IEqualityComparer<TInner?>? equalityComparer)
    : ExpectedValueAssertCondition<TActual, TInner>(expected)
    where TActual : IEnumerable<TInner>
{
    protected override string GetExpectation() => $"to not contain {expected}";

    protected override AssertionResult GetResult(TActual? actualValue, TInner? inner)
        => AssertionResult
            .FailIf(
                () => actualValue is null,
                $"{ActualExpression ?? typeof(TActual).Name} is null")
            .OrFailIf(
                () => actualValue!.Contains(inner, equalityComparer),
                "it was found in the collection");
}