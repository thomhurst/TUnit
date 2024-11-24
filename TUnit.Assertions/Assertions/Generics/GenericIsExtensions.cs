#nullable disable

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.AssertionBuilders.Wrappers;
using TUnit.Assertions.Assertions.Generics.Conditions;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.Assertions.Generics;

public static class GenericIsExtensions
{
    public static GenericEqualToAssertionBuilderWrapper<TActual> IsEqualTo<TActual>(this IValueSource<TActual> valueSource, TActual expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue1 = "")
    {
        var assertionBuilder = valueSource
            .RegisterAssertion(new EqualsExpectedValueAssertCondition<TActual>(expected), [doNotPopulateThisValue1]);
        
        return new GenericEqualToAssertionBuilderWrapper<TActual>(assertionBuilder);
    }
    
    public static InvokableValueAssertionBuilder<TActual> IsEquatableOrEqualTo<TActual>(this IValueSource<TActual> valueSource, TActual expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue1 = "")
    {
        return valueSource
            .RegisterAssertion(new EqualsExpectedValueAssertCondition<TActual>(expected), [doNotPopulateThisValue1]);
    }
    
    public static EquivalentToAssertionBuilderWrapper<TActual, TExpected> IsEquivalentTo<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TActual, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TExpected>(this IValueSource<TActual> valueSource, TExpected expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue1 = "")
    {
        var assertionBuilder = valueSource.RegisterAssertion(new EquivalentToExpectedValueAssertCondition<TActual, TExpected>(expected, doNotPopulateThisValue1)
            , [doNotPopulateThisValue1]);
        
        return new EquivalentToAssertionBuilderWrapper<TActual, TExpected>(assertionBuilder);
    }
    
    public static InvokableValueAssertionBuilder<object> IsSameReferenceAs(this IValueSource<object> valueSource, object expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue1 = "")
    {
        return valueSource.RegisterAssertion(new SameReferenceExpectedValueAssertCondition<object, object>(expected)
            , [doNotPopulateThisValue1]);
    }
    
    public static InvokableValueAssertionBuilder<TActual> IsNull<TActual>(this IValueSource<TActual> valueSource)
    {
        return valueSource.RegisterAssertion(new NullExpectedValueAssertCondition<TActual>()
            , []);
    }
}