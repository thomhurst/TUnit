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

    public EnumerableCount(AssertionBuilder<TActual, TAnd, TOr> assertionBuilder, ConnectorType connectorType,
        BaseAssertCondition<TActual, TAnd, TOr>? otherAssertCondition) : base(connectorType, otherAssertCondition)
    {
        AssertionBuilder = assertionBuilder.AppendExpression("Count");
    }

    public BaseAssertCondition<TActual, TAnd, TOr> EqualTo(int expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
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

    public BaseAssertCondition<TActual, TAnd, TOr> Empty =>
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
    
    public BaseAssertCondition<TActual, TAnd, TOr> GreaterThan(int expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
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

    public BaseAssertCondition<TActual, TAnd, TOr> GreaterThanOrEqualTo(int expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
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

    public BaseAssertCondition<TActual, TAnd, TOr> LessThan(int expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
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

    public BaseAssertCondition<TActual, TAnd, TOr> LessThanOrEqualTo(int expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
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
    
    public BaseAssertCondition<TActual, TAnd, TOr> Negative() => LessThan(0);

    public BaseAssertCondition<TActual, TAnd, TOr> EqualToZero() => EqualTo(0);
    public BaseAssertCondition<TActual, TAnd, TOr> EqualToOne() => EqualTo(1);
    
    public BaseAssertCondition<TActual, TAnd, TOr> Positive() => GreaterThan(0);


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