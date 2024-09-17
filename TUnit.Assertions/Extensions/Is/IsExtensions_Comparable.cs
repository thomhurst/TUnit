#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static partial class IsExtensions
{
    public static InvokableAssertionBuilder<TActual, TAnd, TOr> IsGreaterThan<TActual, TAnd, TOr>(this IValueSource<TActual, TAnd, TOr> valueSource, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
        where TActual : IComparable<TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
    {
        return new DelegateAssertCondition<TActual, TActual>(default, (value, _, _, _) =>
            {
                return value.CompareTo(expected) > 0;
            },
            (value, _, _) => $"{value} was not greater than {expected}")
            .ChainedTo(valueSource.AssertionBuilder, [doNotPopulateThisValue]); }
    
    public static InvokableAssertionBuilder<TActual, TAnd, TOr> IsGreaterThanOrEqualTo<TActual, TAnd, TOr>(this IValueSource<TActual, TAnd, TOr> valueSource, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TActual : IComparable<TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
    {
        return new DelegateAssertCondition<TActual, TActual>(default, (value, _, _, _) =>
            {
                return value.CompareTo(expected) >= 0;
            },
            (value, _, _) => $"{value} was not greater than or equal to {expected}")
            .ChainedTo(valueSource.AssertionBuilder, [doNotPopulateThisValue]); }
    
    public static InvokableAssertionBuilder<TActual, TAnd, TOr> IsLessThan<TActual, TAnd, TOr>(this IValueSource<TActual, TAnd, TOr> valueSource, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TActual : IComparable<TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
    {
        return new DelegateAssertCondition<TActual, TActual>(default, (value, _, _, _) =>
            {
                return value.CompareTo(expected) < 0;
            },
            (value, _, _) => $"{value} was not less than {expected}")
            .ChainedTo(valueSource.AssertionBuilder, [doNotPopulateThisValue]); }
    
    public static InvokableAssertionBuilder<TActual, TAnd, TOr> IsLessThanOrEqualTo<TActual, TAnd, TOr>(this IValueSource<TActual, TAnd, TOr> valueSource, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TActual : IComparable<TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
    {
        return new DelegateAssertCondition<TActual, TActual>(default, (value, _, _, _) =>
            {
                return value.CompareTo(expected) <= 0;
            },
            (value, _, _) => $"{value} was not less than or equal to {expected}")
            .ChainedTo(valueSource.AssertionBuilder, [doNotPopulateThisValue]); }
    
    public static InvokableAssertionBuilder<TActual, TAnd, TOr> IsBetween<TActual, TAnd, TOr>(this IValueSource<TActual, TAnd, TOr> valueSource, TActual lowerBound, TActual upperBound, [CallerArgumentExpression("lowerBound")] string doNotPopulateThisValue1 = "", [CallerArgumentExpression("upperBound")] string doNotPopulateThisValue2 = "")
        where TActual : IComparable<TActual>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
    {
        return new DelegateAssertCondition<TActual, TActual>(default, (value, _, _, _) =>
            {
                return value.CompareTo(lowerBound) >= 0 && value.CompareTo(upperBound) <= 0;
            },
            (value, _, _) => $"{value} was not between {lowerBound} and {upperBound}")
            .ChainedTo(valueSource.AssertionBuilder, [doNotPopulateThisValue1, doNotPopulateThisValue2]); }
}