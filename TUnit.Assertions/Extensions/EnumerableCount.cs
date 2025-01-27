using System.Collections;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public class EnumerableCount<TInner>(IValueSource<IEnumerable<TInner>> valueSource)
{
    public InvokableValueAssertionBuilder<IEnumerable<TInner>> EqualTo(int expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = "")
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<IEnumerable<TInner>, int>(expected, (enumerable, _, self) =>
            {
                if (enumerable is null)
                {
                    self.OverriddenMessage = $"{self.ActualExpression ?? typeof(IEnumerable<TInner>).Name} is null";
                    return false;
                }

                return GetCount(enumerable) == expected;
            },
            (enumerable, _, _) =>
                $"has a count of {GetCount(enumerable)}",
            $"to be equal to {expected}")
            , [doNotPopulateThisValue]);
    }

    public InvokableValueAssertionBuilder<IEnumerable<TInner>> Empty =>
        valueSource.RegisterAssertion(new FuncValueAssertCondition<IEnumerable<TInner>, int>(0, (enumerable, _, self) =>
                {
                    if (enumerable is null)
                    {
                        self.OverriddenMessage = $"{self.ActualExpression ?? typeof(IEnumerable<TInner>).Name} is null";
                        return false;
                    }
                
                    return GetCount(enumerable) == 0;
                },
                (enumerable, _, _) =>
                    $"{enumerable} has a count of {GetCount(enumerable)}",
                $"to be empty")
        , []);
    
    public InvokableValueAssertionBuilder<IEnumerable<TInner>> GreaterThan(int expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = "")
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<IEnumerable<TInner>, int>(expected,
            (enumerable, _, self) =>
            {
                if (enumerable is null)
                {
                    self.OverriddenMessage = $"{self.ActualExpression ?? typeof(IEnumerable<TInner>).Name} is null";
                    return false;
                }
                
                return GetCount(enumerable) > expected;
            },
            (enumerable, _, _) =>
                $"has a count of {GetCount(enumerable)}",
            $"to be greater than {expected}")
            , [doNotPopulateThisValue]);
    }

    public InvokableValueAssertionBuilder<IEnumerable<TInner>> GreaterThanOrEqualTo(int expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = "")
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<IEnumerable<TInner>, int>(expected, (enumerable, _, self) =>
            {
                if (enumerable is null)
                {
                    self.OverriddenMessage = $"{self.ActualExpression ?? typeof(IEnumerable<TInner>).Name} is null";
                    return false;
                }
                
                return GetCount(enumerable) >= expected;
            },
            (enumerable, _, _) =>
                $"has a count of {GetCount(enumerable)}",
            $"to be greater than or equal to {expected}")
            , [doNotPopulateThisValue]);
    }

    public InvokableValueAssertionBuilder<IEnumerable<TInner>> LessThan(int expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = "")
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<IEnumerable<TInner>, int>(expected, (enumerable, _, self) =>
            {
                if (enumerable is null)
                {
                    self.OverriddenMessage = $"{self.ActualExpression ?? typeof(IEnumerable<TInner>).Name} is null";
                    return false;
                }
                
                return GetCount(enumerable) < expected;
            },
            (enumerable, _, _) =>
                $"has a count of {GetCount(enumerable)}",
            $"to be less than {expected}")
            , [doNotPopulateThisValue]);
    }

    public InvokableValueAssertionBuilder<IEnumerable<TInner>> LessThanOrEqualTo(int expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = "")
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<IEnumerable<TInner>, int>(expected, (enumerable, _, self) =>
            {
                if (enumerable is null)
                {
                    self.OverriddenMessage = $"{self.ActualExpression ?? typeof(IEnumerable<TInner>).Name} is null";
                    return false;
                }
                
                return GetCount(enumerable) <= expected;
            },
            (enumerable, _, _) =>
                $"has a count of {GetCount(enumerable)}",
            $"to be less than or equal to {expected}")
            , [doNotPopulateThisValue]);
    }
    
    public InvokableValueAssertionBuilder<IEnumerable<TInner>> Negative() => LessThan(0);

    public InvokableValueAssertionBuilder<IEnumerable<TInner>> EqualToZero() => EqualTo(0);
    public InvokableValueAssertionBuilder<IEnumerable<TInner>> EqualToOne() => EqualTo(1);
    
    public InvokableValueAssertionBuilder<IEnumerable<TInner>> Positive() => GreaterThan(0);


    private int GetCount(IEnumerable<TInner>? actualValue)
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