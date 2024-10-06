namespace TUnit.Assertions.AssertConditions.Collections;

public class EnumerableContainsExpectedValueAssertCondition<TActual, TInner> : ExpectedValueAssertCondition<TActual, TInner>
    where TActual : IEnumerable<TInner>
{
    private readonly IEqualityComparer<TInner?>? _equalityComparer;

    public EnumerableContainsExpectedValueAssertCondition(TInner expected, IEqualityComparer<TInner?>? equalityComparer) : base(expected)
    {
        _equalityComparer = equalityComparer;
    }

    protected override string GetFailureMessage(TActual? actualValue, TInner? inner) => $"{inner} was not found in the collection";

    protected override bool Passes(TActual? actualValue, TInner? inner)
    {
        if (actualValue is null)
        {
            return FailWithMessage($"{ActualExpression ?? typeof(TActual).Name} is null");
        }
        
        return actualValue.Contains(inner, _equalityComparer);
    }
}