namespace TUnit.Assertions.AssertConditions.Collections;

public class EnumerableContainsAssertCondition<TActual, TInner> : AssertCondition<TActual, TInner>
    where TActual : IEnumerable<TInner>
{
    private readonly IEqualityComparer<TInner?>? _equalityComparer;

    public EnumerableContainsAssertCondition(TInner expected, IEqualityComparer<TInner?>? equalityComparer) : base(expected)
    {
        _equalityComparer = equalityComparer;
    }

    protected internal override string GetFailureMessage() => $"{ExpectedValue} was not found in the collection";

    protected override bool Passes(TActual? actualValue, Exception? exception)
    {
        if (actualValue is null)
        {
            OverriddenMessage = $"{ActualExpression ?? typeof(TActual).Name} is null";
            return false;
        }
        
        return actualValue.Contains(ExpectedValue, _equalityComparer);
    }
}