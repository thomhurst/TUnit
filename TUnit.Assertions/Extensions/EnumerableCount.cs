using System.Collections;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public class EnumerableCount<TActual, TAnd, TOr>
    where TActual : IEnumerable?
    where TAnd : IAnd<TActual, TAnd, TOr>
    where TOr : IOr<TActual, TAnd, TOr>
{
    protected internal AssertionBuilder<TActual, TAnd, TOr> AssertionBuilder { get; }

    public EnumerableCount(AssertionBuilder<TActual, TAnd, TOr> assertionBuilder)
    {
        AssertionBuilder = assertionBuilder.AppendExpression("Count");
    }

    public InvokableAssertionBuilder<TActual, TAnd, TOr> EqualTo(int expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return new DelegateAssertCondition<TActual, int>(expected, (enumerable, expected, _, self) =>
            {
                if (enumerable is null)
                {
                    self.OverriddenMessage = $"{self.RawActualExpression ?? typeof(TActual).Name} is null";
                    return false;
                }

                return GetCount(enumerable) == expected;
            },
            (enumerable, _, _) =>
                $"{enumerable} has a count of {GetCount(enumerable)} but expected to be equal to {expected}")
            .ChainedTo(AssertionBuilder, [doNotPopulateThisValue]);
    }

    public InvokableAssertionBuilder<TActual, TAnd, TOr> Empty =>
        new DelegateAssertCondition<TActual, int>(0, (enumerable, expected, _, self) =>
                {
                    if (enumerable is null)
                    {
                        self.OverriddenMessage = $"{self.RawActualExpression ?? typeof(TActual).Name} is null";
                        return false;
                    }
                
                    return GetCount(enumerable) == expected;
                },
                (enumerable, _, _) =>
                    $"{enumerable} has a count of {GetCount(enumerable)} but expected to be equal to {0}")
        .ChainedTo(AssertionBuilder, []);
    
    public InvokableAssertionBuilder<TActual, TAnd, TOr> GreaterThan(int expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return new DelegateAssertCondition<TActual, int>(expected,
            (enumerable, _, _, self) =>
            {
                if (enumerable is null)
                {
                    self.OverriddenMessage = $"{self.RawActualExpression ?? typeof(TActual).Name} is null";
                    return false;
                }
                
                return GetCount(enumerable) > expected;
            },
            (enumerable, _, _) =>
                $"{enumerable} has a count of {GetCount(enumerable)} but expected to be greater than {expected}")
            .ChainedTo(AssertionBuilder, [doNotPopulateThisValue]);
    }

    public InvokableAssertionBuilder<TActual, TAnd, TOr> GreaterThanOrEqualTo(int expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return new DelegateAssertCondition<TActual, int>(expected, (enumerable, expected, _, self) =>
            {
                if (enumerable is null)
                {
                    self.OverriddenMessage = $"{self.RawActualExpression ?? typeof(TActual).Name} is null";
                    return false;
                }
                
                return GetCount(enumerable) >= expected;
            },
            (enumerable, _, _) =>
                $"{enumerable} has a count of {GetCount(enumerable)} but expected to be greater than or equal to {expected}")
            .ChainedTo(AssertionBuilder, [doNotPopulateThisValue]);
    }

    public InvokableAssertionBuilder<TActual, TAnd, TOr> LessThan(int expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return new DelegateAssertCondition<TActual, int>(expected, (enumerable, expected, _, self) =>
            {
                if (enumerable is null)
                {
                    self.OverriddenMessage = $"{self.RawActualExpression ?? typeof(TActual).Name} is null";
                    return false;
                }
                
                return GetCount(enumerable) < expected;
            },
            (enumerable, _, _) =>
                $"{enumerable} has a count of {GetCount(enumerable)} but expected to be less than {expected}")
            .ChainedTo(AssertionBuilder, [doNotPopulateThisValue]);
    }

    public InvokableAssertionBuilder<TActual, TAnd, TOr> LessThanOrEqualTo(int expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return new DelegateAssertCondition<TActual, int>(expected, (enumerable, expected, _, self) =>
            {
                if (enumerable is null)
                {
                    self.OverriddenMessage = $"{self.RawActualExpression ?? typeof(TActual).Name} is null";
                    return false;
                }
                
                return GetCount(enumerable) <= expected;
            },
            (enumerable, _, _) =>
                $"{enumerable} has a count of {GetCount(enumerable)} but expected to be less than or equal to {expected}")
            .ChainedTo(AssertionBuilder, [doNotPopulateThisValue]);
    }
    
    public InvokableAssertionBuilder<TActual, TAnd, TOr> Negative() => LessThan(0);

    public InvokableAssertionBuilder<TActual, TAnd, TOr> EqualToZero() => EqualTo(0);
    public InvokableAssertionBuilder<TActual, TAnd, TOr> EqualToOne() => EqualTo(1);
    
    public InvokableAssertionBuilder<TActual, TAnd, TOr> Positive() => GreaterThan(0);


    private int GetCount(TActual? actualValue)
    {
        if (actualValue is ICollection collection)
        {
            return collection.Count;
        }

        if (actualValue is IList list)
        {
            return list.Count;
        }

        return actualValue?.Cast<object>().Count() ?? 0;
    }
}