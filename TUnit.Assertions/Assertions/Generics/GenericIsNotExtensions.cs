#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Assertions.Generics.Conditions;

namespace TUnit.Assertions.Extensions;

public static class GenericIsNotExtensions
{
    public static InvokableValueAssertionBuilder<TActual> IsNotEqualTo<TActual>(this IValueSource<TActual> valueSource, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "") 
        where TActual : class
    {
        return valueSource.RegisterAssertion(new NotEqualsExpectedValueAssertCondition<TActual>(expected)
            , [doNotPopulateThisValue]);
    }
    
    public static InvokableValueAssertionBuilder<TActual> IsNotNull<TActual>(this IValueSource<TActual> valueSource)
    {
        return valueSource!.RegisterAssertion(new NotNullExpectedValueAssertCondition<TActual>()
            , []);
    }
    
    public static InvokableValueAssertionBuilder<TActual> IsNotEquatableOrEqualTo<TActual>(this IValueSource<TActual> valueSource, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return valueSource.RegisterAssertion(new NotEqualsExpectedValueAssertCondition<TActual>(expected)
            , [doNotPopulateThisValue]);
    }
    
    public static InvokableValueAssertionBuilder<TActual> IsDefault<TActual>(this IValueSource<TActual> valueSource)
    {
        return valueSource.RegisterAssertion(new DefaultExpectedValueAssertCondition<TActual>()
            , []);
    }
    
    public static InvokableValueAssertionBuilder<TActual> IsNotDefault<TActual>(this IValueSource<TActual> valueSource)
    {
        return valueSource.RegisterAssertion(new NotDefaultExpectedValueAssertCondition<TActual>()
            , []);
    }
}