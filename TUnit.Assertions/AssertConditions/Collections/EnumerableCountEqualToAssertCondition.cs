using System.Collections;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.Collections;

public class EnumerableCountEqualToAssertCondition<TActual, TAnd, TOr> : AssertCondition<TActual, int, TAnd, TOr>
    where TActual : IEnumerable
    where TAnd : IAnd<TActual, TAnd, TOr>
    where TOr : IOr<TActual, TAnd, TOr>
{
    public EnumerableCountEqualToAssertCondition(AssertionBuilder<TActual, TAnd, TOr> assertionBuilder, int expected) : base(assertionBuilder, expected)
    {
    }

    protected override string DefaultMessage => $"Length is {GetCount(ActualValue)} instead of {ExpectedValue}";
    
    protected internal override bool Passes(TActual? actualValue, Exception? exception)
    {
        if (actualValue is null)
        {
            WithMessage((_, _) => $"{AssertionBuilder.RawActualExpression ?? typeof(TActual).Name} is null");
            return false;
        }
        
        return GetCount(actualValue) == ExpectedValue;
    }

    private int GetCount(TActual? actualValue)
    {
        if (actualValue is ICollection collection)
        {
            return collection.Count;
        }
        
        return actualValue?.Cast<object>().Count() ?? 0;
    }
}