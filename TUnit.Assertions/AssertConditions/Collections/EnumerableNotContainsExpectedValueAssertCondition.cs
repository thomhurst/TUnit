namespace TUnit.Assertions.AssertConditions.Collections;

public class EnumerableNotContainsExpectedValueAssertCondition<TActual, TInner>(
    TInner expected,
    IEqualityComparer<TInner?>? equalityComparer)
    : ExpectedValueAssertCondition<TActual, TInner>(expected)
    where TActual : IEnumerable<TInner>
{
    protected override string GetFailureMessage(TActual? actualValue, TInner? expectedValue) => $"{expectedValue} was not in the collection";

    protected override bool Passes(TActual? actualValue, TInner? inner)
    {
        if (actualValue is null)
        {
            return FailWithMessage($"{ActualExpression ?? typeof(TActual).Name} is null");
        }
        
        return !actualValue.Contains(ExpectedValue, equalityComparer);
    }
}