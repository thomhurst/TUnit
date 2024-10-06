﻿using System.Collections;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public class EnumerableCount<TActual>
    where TActual : IEnumerable?
{
    private readonly IValueSource<TActual> _valueSource;
    protected internal AssertionBuilder<TActual> AssertionBuilder { get; }

    public EnumerableCount(IValueSource<TActual> valueSource)
    {
        _valueSource = valueSource;
        AssertionBuilder = valueSource.AssertionBuilder.AppendExpression("HasCount");
    }

    public InvokableValueAssertionBuilder<TActual> EqualTo(int expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return _valueSource.RegisterAssertion(new FuncValueAssertCondition<TActual, int>(expected, (enumerable, _, self) =>
            {
                if (enumerable is null)
                {
                    self.OverriddenMessage = $"{self.ActualExpression ?? typeof(TActual).Name} is null";
                    return false;
                }

                return GetCount(enumerable) == expected;
            },
            (enumerable, _, _) =>
                $"{enumerable} has a count of {GetCount(enumerable)} but expected to be equal to {expected}")
            , [doNotPopulateThisValue]);
    }

    public InvokableValueAssertionBuilder<TActual> Empty =>
        _valueSource.RegisterAssertion(new FuncValueAssertCondition<TActual, int>(0, (enumerable, _, self) =>
                {
                    if (enumerable is null)
                    {
                        self.OverriddenMessage = $"{self.ActualExpression ?? typeof(TActual).Name} is null";
                        return false;
                    }
                
                    return GetCount(enumerable) == 0;
                },
                (enumerable, _, _) =>
                    $"{enumerable} has a count of {GetCount(enumerable)} but expected to be equal to {0}")
        , []);
    
    public InvokableValueAssertionBuilder<TActual> GreaterThan(int expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return _valueSource.RegisterAssertion(new FuncValueAssertCondition<TActual, int>(expected,
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
                $"{enumerable} has a count of {GetCount(enumerable)} but expected to be greater than {expected}")
            , [doNotPopulateThisValue]);
    }

    public InvokableValueAssertionBuilder<TActual> GreaterThanOrEqualTo(int expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return _valueSource.RegisterAssertion(new FuncValueAssertCondition<TActual, int>(expected, (enumerable, _, self) =>
            {
                if (enumerable is null)
                {
                    self.OverriddenMessage = $"{self.ActualExpression ?? typeof(TActual).Name} is null";
                    return false;
                }
                
                return GetCount(enumerable) >= expected;
            },
            (enumerable, _, _) =>
                $"{enumerable} has a count of {GetCount(enumerable)} but expected to be greater than or equal to {expected}")
            , [doNotPopulateThisValue]);
    }

    public InvokableValueAssertionBuilder<TActual> LessThan(int expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return _valueSource.RegisterAssertion(new FuncValueAssertCondition<TActual, int>(expected, (enumerable, _, self) =>
            {
                if (enumerable is null)
                {
                    self.OverriddenMessage = $"{self.ActualExpression ?? typeof(TActual).Name} is null";
                    return false;
                }
                
                return GetCount(enumerable) < expected;
            },
            (enumerable, _, _) =>
                $"{enumerable} has a count of {GetCount(enumerable)} but expected to be less than {expected}")
            , [doNotPopulateThisValue]);
    }

    public InvokableValueAssertionBuilder<TActual> LessThanOrEqualTo(int expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return _valueSource.RegisterAssertion(new FuncValueAssertCondition<TActual, int>(expected, (enumerable, _, self) =>
            {
                if (enumerable is null)
                {
                    self.OverriddenMessage = $"{self.ActualExpression ?? typeof(TActual).Name} is null";
                    return false;
                }
                
                return GetCount(enumerable) <= expected;
            },
            (enumerable, _, _) =>
                $"{enumerable} has a count of {GetCount(enumerable)} but expected to be less than or equal to {expected}")
            , [doNotPopulateThisValue]);
    }
    
    public InvokableValueAssertionBuilder<TActual> Negative() => LessThan(0);

    public InvokableValueAssertionBuilder<TActual> EqualToZero() => EqualTo(0);
    public InvokableValueAssertionBuilder<TActual> EqualToOne() => EqualTo(1);
    
    public InvokableValueAssertionBuilder<TActual> Positive() => GreaterThan(0);


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