using System.Collections;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public class EnumerableCount<TActual, TInner>(IValueSource<TActual> valueSource) where TActual : IEnumerable<TInner>
{
    public InvokableValueAssertion<TActual> EqualTo(int expected, [CallerArgumentExpression(nameof(expected))] string? doNotPopulateThisValue = null)
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<TActual, int>(expected, (enumerable, _, self) =>
            {
                if (enumerable is null)
                {
                    self.OverriddenMessage = $"{self.ActualExpression ?? typeof(TActual).Name} is null";
                    return false;
                }

                return GetCount(enumerable) == expected;
            },
            (enumerable, _, _) =>
                $"has a count of {GetCount(enumerable)}",
            $"to be equal to {expected}")
            , [doNotPopulateThisValue]);
    }

    public InvokableValueAssertion<TActual> Empty =>
        valueSource.RegisterAssertion(new FuncValueAssertCondition<TActual, int>(0, (enumerable, _, self) =>
                {
                    if (enumerable is null)
                    {
                        self.OverriddenMessage = $"{self.ActualExpression ?? typeof(TActual).Name} is null";
                        return false;
                    }

                    return GetCount(enumerable) == 0;
                },
                (enumerable, _, _) =>
                    $"{enumerable} has a count of {GetCount(enumerable)}",
                $"to be empty")
        , []);

    public InvokableValueAssertion<TActual> GreaterThan(int expected, [CallerArgumentExpression(nameof(expected))] string? doNotPopulateThisValue = null)
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<TActual, int>(expected,
            (enumerable, _, self) =>
            {
                if (enumerable is null)
                {
                    self.OverriddenMessage = $"{self.ActualExpression ?? typeof(TActual).Name} is null";
                    return false;
                }

                return GetCount(enumerable) > expected;
            },
            (enumerable, _, _) =>
                $"has a count of {GetCount(enumerable)}",
            $"to be greater than {expected}")
            , [doNotPopulateThisValue]);
    }

    public InvokableValueAssertion<TActual> GreaterThanOrEqualTo(int expected, [CallerArgumentExpression(nameof(expected))] string? doNotPopulateThisValue = null)
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<TActual, int>(expected, (enumerable, _, self) =>
            {
                if (enumerable is null)
                {
                    self.OverriddenMessage = $"{self.ActualExpression ?? typeof(TActual).Name} is null";
                    return false;
                }

                return GetCount(enumerable) >= expected;
            },
            (enumerable, _, _) =>
                $"has a count of {GetCount(enumerable)}",
            $"to be greater than or equal to {expected}")
            , [doNotPopulateThisValue]);
    }

    public InvokableValueAssertion<TActual> LessThan(int expected, [CallerArgumentExpression(nameof(expected))] string? doNotPopulateThisValue = null)
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<TActual, int>(expected, (enumerable, _, self) =>
            {
                if (enumerable is null)
                {
                    self.OverriddenMessage = $"{self.ActualExpression ?? typeof(TActual).Name} is null";
                    return false;
                }

                return GetCount(enumerable) < expected;
            },
            (enumerable, _, _) =>
                $"has a count of {GetCount(enumerable)}",
            $"to be less than {expected}")
            , [doNotPopulateThisValue]);
    }

    public InvokableValueAssertion<TActual> LessThanOrEqualTo(int expected, [CallerArgumentExpression(nameof(expected))] string? doNotPopulateThisValue = null)
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<TActual, int>(expected, (enumerable, _, self) =>
            {
                if (enumerable is null)
                {
                    self.OverriddenMessage = $"{self.ActualExpression ?? typeof(TActual).Name} is null";
                    return false;
                }

                return GetCount(enumerable) <= expected;
            },
            (enumerable, _, _) =>
                $"has a count of {GetCount(enumerable)}",
            $"to be less than or equal to {expected}")
            , [doNotPopulateThisValue]);
    }

    public InvokableValueAssertion<TActual> Negative() => LessThan(0);

    public InvokableValueAssertion<TActual> EqualToZero() => EqualTo(0);
    public InvokableValueAssertion<TActual> EqualToOne() => EqualTo(1);

    public InvokableValueAssertion<TActual> Positive() => GreaterThan(0);


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
