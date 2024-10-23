#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Comparable;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.AssertionBuilders.Wrappers;

namespace TUnit.Assertions.Extensions;

public static class ComparableIsNotExtensions
{
    public static InvokableValueAssertionBuilder<TActual> IsNotGreaterThan<TActual>(this IValueSource<TActual> valueSource, TActual expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = "")
        where TActual : IComparable<TActual>
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<TActual, TActual>(default, (value, _, _) =>
            {
                return value.CompareTo(expected) <= 0;
            },
            (value, _, _) => $"{value} was greater than {expected}",
            $"to not be greater than {expected}")
            , [doNotPopulateThisValue]); }
    
    public static InvokableValueAssertionBuilder<TActual> IsNotGreaterThanOrEqualTo<TActual>(this IValueSource<TActual> valueSource, TActual expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = "") 
        where TActual : IComparable<TActual>
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<TActual, TActual>(default, (value, _, _) =>
            {
                return value.CompareTo(expected) < 0;
            },
            (value, _, _) => $"{value} was greater than or equal to {expected}",
            $"to not be greater than or equal to {expected}")
            , [doNotPopulateThisValue]); }
    
    public static InvokableValueAssertionBuilder<TActual> IsNotLessThan<TActual>(this IValueSource<TActual> valueSource, TActual expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = "") 
        where TActual : IComparable<TActual>
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<TActual, TActual>(default, (value, _, _) =>
            {
                return value.CompareTo(expected) >= 0;
            },
            (value, _, _) => $"{value} was less than {expected}",
            $"to not be less than {expected}")
            , [doNotPopulateThisValue]); }
    
    public static InvokableValueAssertionBuilder<TActual> IsNotLessThanOrEqualTo<TActual>(this IValueSource<TActual> valueSource, TActual expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = "") 
        where TActual : IComparable<TActual>
    {
        return valueSource.RegisterAssertion(new FuncValueAssertCondition<TActual, TActual>(default, (value, _, _) =>
            {
                return value.CompareTo(expected) > 0;
            },
            (value, _, _) => $"{value} was less than or equal to {expected}",
            $"to not be less than or equal to {expected}")
            , [doNotPopulateThisValue]); }
    
    public static NotBetweenAssertionBuilderWrapper<TActual> IsNotBetween<TActual>(this IValueSource<TActual> valueSource, TActual lowerBound, TActual upperBound, [CallerArgumentExpression(nameof(lowerBound))] string doNotPopulateThisValue1 = "", [CallerArgumentExpression(nameof(upperBound))] string doNotPopulateThisValue2 = "")
        where TActual : IComparable<TActual>
    {
        var assertionBuilder = valueSource.RegisterAssertion(new NotBetweenAssertCondition<TActual>(lowerBound, upperBound)
            , [doNotPopulateThisValue1, doNotPopulateThisValue2]);
        
        return new NotBetweenAssertionBuilderWrapper<TActual>(assertionBuilder);
    }
}