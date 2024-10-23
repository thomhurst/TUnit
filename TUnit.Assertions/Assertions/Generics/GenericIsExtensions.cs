#nullable disable

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.AssertionBuilders.Wrappers;
using TUnit.Assertions.Assertions.Generics.Conditions;

namespace TUnit.Assertions.Extensions;

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
    
    public static EquivalentToAssertionBuilderWrapper<TActual> IsEquivalentTo<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TActual>(this IValueSource<TActual> valueSource, TActual expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue1 = "")
    {
        var assertionBuilder = valueSource.RegisterAssertion(new EquivalentToExpectedValueAssertCondition<TActual>(expected, doNotPopulateThisValue1)
            , [doNotPopulateThisValue1]);
        
        return new EquivalentToAssertionBuilderWrapper<TActual>(assertionBuilder);
    }
    
    public static InvokableValueAssertionBuilder<object> IsSameReference(this IValueSource<object> valueSource, object expected, [CallerArgumentExpression(nameof(expected))] string doNotPopulateThisValue1 = "")
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