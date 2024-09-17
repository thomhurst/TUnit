#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static partial class IsExtensions
{
    public static AssertionBuilder<TActual, TAnd, TOr> IsEqualTo<TActual, TAnd, TOr>(this AssertionBuilder<TActual, TAnd, TOr> assertionBuilder, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "")
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
    {
        return new EqualsAssertCondition<TActual, TAnd, TOr>(assertionBuilder.AppendCallerMethod(doNotPopulateThisValue1), expected)
            .ChainedTo(assertionBuilder);
    }
    
    public static AssertionBuilder<object, TAnd, TOr> IsEquivalentTo<TAnd, TOr>(this AssertionBuilder<object, TAnd, TOr> assertionBuilder, object expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "")
        where TAnd : IAnd<object, TAnd, TOr>
        where TOr : IOr<object, TAnd, TOr>
    {
        return new EquivalentToAssertCondition<object, TAnd, TOr>(assertionBuilder.AppendCallerMethod(doNotPopulateThisValue1), expected)
            .ChainedTo(assertionBuilder);
    }
    
    public static AssertionBuilder<object, TAnd, TOr> IsSameReference<TAnd, TOr>(this AssertionBuilder<object, TAnd, TOr> assertionBuilder, object expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "")
        where TAnd : IAnd<object, TAnd, TOr>
        where TOr : IOr<object, TAnd, TOr>
    {
        return new SameReferenceAssertCondition<object, object, TAnd,TOr>(assertionBuilder.AppendCallerMethod(doNotPopulateThisValue1), expected)
            .ChainedTo(assertionBuilder);
    }
    
    public static AssertionBuilder<TActual, TAnd, TOr> IsNull<TActual, TAnd, TOr>(this AssertionBuilder<TActual, TAnd, TOr> assertionBuilder)
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
    {
        return new NullAssertCondition<TActual, TAnd, TOr>(assertionBuilder.AppendCallerMethod(string.Empty))
            .ChainedTo(assertionBuilder);
    }

    public static AssertionBuilder<TActual, TAnd, TOr> IsTypeOf<TActual, TAnd, TOr>(this AssertionBuilder<TActual, TAnd, TOr> assertionBuilder, Type type) where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
    {
        return new TypeOfAssertCondition<TActual, TAnd, TOr>(
            assertionBuilder.AppendCallerMethod(type.FullName), type)
            .ChainedTo(assertionBuilder);
    }

    public static AssertionBuilder<TActual, TAnd, TOr> IsAssignableTo<TActual, TExpected, TAnd, TOr>(this AssertionBuilder<TActual, TAnd, TOr> assertionBuilder) 
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
    {
        return new DelegateAssertCondition<TActual, TExpected, TAnd, TOr>(
            assertionBuilder.AppendCallerMethod(typeof(TExpected).FullName),
            default,
            (value, _, _, _) => value!.GetType().IsAssignableTo(typeof(TExpected)),
            (actual, _) => $"{actual?.GetType()} is not assignable to {typeof(TExpected).Name}")
            .ChainedTo(assertionBuilder); }

    public static AssertionBuilder<TActual, TAnd, TOr> IsAssignableFrom<TActual, TExpected, TAnd, TOr>(this AssertionBuilder<TActual, TAnd, TOr> assertionBuilder) 
        where TExpected : TActual
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
    {
        return new DelegateAssertCondition<TActual, TExpected, TAnd, TOr>(
            assertionBuilder.AppendCallerMethod(typeof(TExpected).FullName),
            default,
            (value, _, _, _) => value!.GetType().IsAssignableFrom(typeof(TExpected)),
            (actual, _) => $"{actual?.GetType()} is not assignable from {typeof(TExpected).Name}")
            .ChainedTo(assertionBuilder); }
}