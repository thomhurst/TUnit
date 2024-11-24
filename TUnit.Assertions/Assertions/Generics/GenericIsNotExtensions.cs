#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.AssertionBuilders.Wrappers;
using TUnit.Assertions.Assertions.Generics.Conditions;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.Assertions.Generics;

public static class GenericIsNotExtensions
{
    public static GenericNotEqualToAssertionBuilderWrapper<TActual> IsNotEqualTo<TActual>(this IValueSource<TActual> valueSource, TActual expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = "") 
    {
        var assertionBuilder = valueSource.RegisterAssertion(new NotEqualsExpectedValueAssertCondition<TActual>(expected)
            , [doNotPopulateThisValue]);
        
        return new GenericNotEqualToAssertionBuilderWrapper<TActual>(assertionBuilder);
    }
    
    public static InvokableValueAssertionBuilder<TActual> IsNotNull<TActual>(this IValueSource<TActual> valueSource)
    {
        return valueSource!.RegisterAssertion(new NotNullExpectedValueAssertCondition<TActual>()
            , []);
    }
    
    public static InvokableValueAssertionBuilder<TActual> IsNotEquatableOrEqualTo<TActual>(this IValueSource<TActual> valueSource, TActual expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue = "")
    {
        return valueSource.RegisterAssertion(new NotEqualsExpectedValueAssertCondition<TActual>(expected)
            , [doNotPopulateThisValue]);
    }
    
    public static InvokableValueAssertionBuilder<object> IsNotSameReferenceAs(this IValueSource<object> valueSource, object expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue1 = "")
    {
        return valueSource.RegisterAssertion(new NotSameReferenceExpectedValueAssertCondition<object, object>(expected)
            , [doNotPopulateThisValue1]);
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