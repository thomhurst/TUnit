using System.Collections;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public class EnumerableCount<TActual, TAnd, TOr> : Connector<TActual, TAnd, TOr> 
    where TActual : IEnumerable?
    where TAnd : IAnd<TActual, TAnd, TOr>
    where TOr : IOr<TActual, TAnd, TOr>
{
    protected internal AssertionBuilder<TActual, TAnd, TOr> AssertionBuilder { get; }

    public EnumerableCount(AssertionBuilder<TActual, TAnd, TOr> assertionBuilder, ChainType chainType,
        BaseAssertCondition<TActual>? otherAssertCondition) : base(chainType, otherAssertCondition)
    {
        AssertionBuilder = assertionBuilder.AppendExpression("Count");
    }

    public BaseAssertCondition<TActual> EqualTo(int expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return Combine(new DelegateAssertCondition<TActual, int, TAnd, TOr>(AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), expected, (enumerable, expected, _, self) =>
            {
                if (enumerable is null)
                {
                    self.WithMessage((_, _) => $"{AssertionBuilder.RawActualExpression ?? typeof(TActual).Name} is null");
                    return false;
                }

                return GetCount(enumerable) == expected;
            },
            (enumerable, _) =>
                $"{enumerable} has a count of {GetCount(enumerable)} but expected to be equal to {expected}")
        );
    }

    public BaseAssertCondition<TActual> Empty =>
        Combine(new DelegateAssertCondition<TActual, int, TAnd, TOr>(AssertionBuilder.AppendCallerMethod(null), 0, (enumerable, expected, _, self) =>
            {
                if (enumerable is null)
                {
                    self.WithMessage((_, _) => $"{AssertionBuilder.RawActualExpression ?? typeof(TActual).Name} is null");
                    return false;
                }
                
                return GetCount(enumerable) == expected;
            },
            (enumerable, _) =>
                $"{enumerable} has a count of {GetCount(enumerable)} but expected to be equal to {0}")
        );
    
    public BaseAssertCondition<TActual> GreaterThan(int expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return Combine(new DelegateAssertCondition<TActual, int, TAnd, TOr>(
            AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue),
            expected,
            (enumerable, _, _, self) =>
            {
                if (enumerable is null)
                {
                    self.WithMessage((_, _) => $"{AssertionBuilder.RawActualExpression ?? typeof(TActual).Name} is null");
                    return false;
                }
                
                return GetCount(enumerable) > expected;
            },
            (enumerable, _) =>
                $"{enumerable} has a count of {GetCount(enumerable)} but expected to be greater than {expected}")
        );
    }

    public BaseAssertCondition<TActual> GreaterThanOrEqualTo(int expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return Combine(new DelegateAssertCondition<TActual, int, TAnd, TOr>(AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), expected, (enumerable, expected, _, self) =>
            {
                if (enumerable is null)
                {
                    self.WithMessage((_, _) => $"{AssertionBuilder.RawActualExpression ?? typeof(TActual).Name} is null");
                    return false;
                }
                
                return GetCount(enumerable) >= expected;
            },
            (enumerable, _) =>
                $"{enumerable} has a count of {GetCount(enumerable)} but expected to be greater than or equal to {expected}")
        );
    }

    public BaseAssertCondition<TActual> LessThan(int expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return Combine(new DelegateAssertCondition<TActual, int, TAnd, TOr>(AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), expected, (enumerable, expected, _, self) =>
            {
                if (enumerable is null)
                {
                    self.WithMessage((_, _) => $"{AssertionBuilder.RawActualExpression ?? typeof(TActual).Name} is null");
                    return false;
                }
                
                return GetCount(enumerable) < expected;
            },
            (enumerable, _) =>
                $"{enumerable} has a count of {GetCount(enumerable)} but expected to be less than {expected}")
        );
    }

    public BaseAssertCondition<TActual> LessThanOrEqualTo(int expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return Combine(new DelegateAssertCondition<TActual, int, TAnd, TOr>(AssertionBuilder.AppendCallerMethod(doNotPopulateThisValue), expected, (enumerable, expected, _, self) =>
            {
                if (enumerable is null)
                {
                    self.WithMessage((_, _) => $"{AssertionBuilder.RawActualExpression ?? typeof(TActual).Name} is null");
                    return false;
                }
                
                return GetCount(enumerable) <= expected;
            },
            (enumerable, _) =>
                $"{enumerable} has a count of {GetCount(enumerable)} but expected to be less than or equal to {expected}")
        );
    }
    
    public BaseAssertCondition<TActual> Negative() => LessThan(0);

    public BaseAssertCondition<TActual> EqualToZero() => EqualTo(0);
    public BaseAssertCondition<TActual> EqualToOne() => EqualTo(1);
    
    public BaseAssertCondition<TActual> Positive() => GreaterThan(0);


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