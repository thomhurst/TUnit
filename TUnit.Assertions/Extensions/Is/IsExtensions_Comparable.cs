#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static partial class IsExtensions
{
    public static InvokableValueAssertionBuilder<TActual> IsGreaterThan<TActual>(this IValueSource<TActual> valueSource, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TActual : IComparable<TActual>
    {
        return valueSource.RegisterAssertion(new DelegateAssertCondition<TActual, TActual>(default, (value, _, _, _) =>
            {
                return value.CompareTo(expected) > 0;
            },
            (value, _, _) => $"{value} was not greater than {expected}")
            , [doNotPopulateThisValue]); }
    
    public static InvokableValueAssertionBuilder<TActual> IsGreaterThanOrEqualTo<TActual>(this IValueSource<TActual> valueSource, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TActual : IComparable<TActual>
    {
        return valueSource.RegisterAssertion(new DelegateAssertCondition<TActual, TActual>(default, (value, _, _, _) =>
            {
                return value.CompareTo(expected) >= 0;
            },
            (value, _, _) => $"{value} was not greater than or equal to {expected}")
            , [doNotPopulateThisValue]); }
    
    public static InvokableValueAssertionBuilder<TActual> IsLessThan<TActual>(this IValueSource<TActual> valueSource, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TActual : IComparable<TActual>
    {
        return valueSource.RegisterAssertion(new DelegateAssertCondition<TActual, TActual>(default, (value, _, _, _) =>
            {
                return value.CompareTo(expected) < 0;
            },
            (value, _, _) => $"{value} was not less than {expected}")
            , [doNotPopulateThisValue]); }
    
    public static InvokableValueAssertionBuilder<TActual> IsLessThanOrEqualTo<TActual>(this IValueSource<TActual> valueSource, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TActual : IComparable<TActual>
    {
        return valueSource.RegisterAssertion(new DelegateAssertCondition<TActual, TActual>(default, (value, _, _, _) =>
            {
                return value.CompareTo(expected) <= 0;
            },
            (value, _, _) => $"{value} was not less than or equal to {expected}")
            , [doNotPopulateThisValue]); }
    
    public static InvokableValueAssertionBuilder<TActual> IsBetween<TActual>(this IValueSource<TActual> valueSource, TActual lowerBound, TActual upperBound, [CallerArgumentExpression("lowerBound")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("upperBound")] string doNotPopulateThisValue2 = "")
        where TActual : IComparable<TActual>
    {
        return valueSource.RegisterAssertion(new DelegateAssertCondition<TActual, TActual>(default, (value, _, _, _) =>
            {
                return value.CompareTo(lowerBound) >= 0 && value.CompareTo(upperBound) <= 0;
            },
            (value, _, _) => $"{value} was not between {lowerBound} and {upperBound}")
            , [doNotPopulateThisValue1, doNotPopulateThisValue2]); }
}