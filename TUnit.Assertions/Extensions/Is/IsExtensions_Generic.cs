#nullable disable

using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Generic;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static partial class IsExtensions
{
    public static TOutput IsEqualTo<TAssertionBuilder, TOutput, TActual, TAnd, TOr>(this TAssertionBuilder assertionBuilder, TActual expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "")
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
        where TOutput : InvokableAssertionBuilder<TActual, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<TActual, TAnd, TOr>, IOutputsChain<TOutput, TActual>
    {
        return new EqualsAssertCondition<TActual, TAnd, TOr>(assertionBuilder.AppendCallerMethod(doNotPopulateThisValue1), expected)
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder);
    }
    
    public static TOutput IsEquivalentTo<TAssertionBuilder, TOutput, TAnd, TOr>(this TAssertionBuilder assertionBuilder, object expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "")
        where TAnd : IAnd<object, TAnd, TOr>
        where TOr : IOr<object, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<object, TAnd, TOr>, IOutputsChain<TOutput, object>
        where TOutput : InvokableAssertionBuilder<object, TAnd, TOr>
    {
        return new EquivalentToAssertCondition<object, TAnd, TOr>(assertionBuilder.AppendCallerMethod(doNotPopulateThisValue1), expected)
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder);
    }
    
    public static TOutput IsSameReference<TAssertionBuilder, TOutput, TAnd, TOr>(this TAssertionBuilder assertionBuilder, object expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue1 = "")
        where TAnd : IAnd<object, TAnd, TOr>
        where TOr : IOr<object, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<object, TAnd, TOr>, IOutputsChain<TOutput, object>
        where TOutput : InvokableAssertionBuilder<object, TAnd, TOr>
    {
        return new SameReferenceAssertCondition<object, object, TAnd,TOr>(assertionBuilder.AppendCallerMethod(doNotPopulateThisValue1), expected)
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder);
    }
    
    public static TOutput IsNull<TAssertionBuilder, TOutput, TActual, TAnd, TOr>(this TAssertionBuilder assertionBuilder)
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
        where TOutput : InvokableAssertionBuilder<TActual, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<TActual, TAnd, TOr>, IOutputsChain<TOutput, TActual>
    {
        return new NullAssertCondition<TActual, TAnd, TOr>(assertionBuilder.AppendCallerMethod(string.Empty))
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder);
    }

    public static TOutput IsTypeOf<TAssertionBuilder, TOutput, TActual, TAnd, TOr>(this TAssertionBuilder assertionBuilder, Type type) where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
        where TOutput : InvokableAssertionBuilder<TActual, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<TActual, TAnd, TOr>, IOutputsChain<TOutput, TActual>
    {
        return new TypeOfAssertCondition<TActual, TAnd, TOr>(
            assertionBuilder.AppendCallerMethod(type.FullName), type)
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder);
    }

    public static TOutput IsAssignableTo<TAssertionBuilder, TOutput, TActual, TExpected, TAnd, TOr>(this TAssertionBuilder assertionBuilder) 
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
        where TOutput : InvokableAssertionBuilder<TActual, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<TActual, TAnd, TOr>, IOutputsChain<TOutput, TActual>
    {
        return new DelegateAssertCondition<TActual, TExpected, TAnd, TOr>(
            assertionBuilder.AppendCallerMethod(typeof(TExpected).FullName),
            default,
            (value, _, _, _) => value!.GetType().IsAssignableTo(typeof(TExpected)),
            (actual, _) => $"{actual?.GetType()} is not assignable to {typeof(TExpected).Name}")
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder); }

    public static TOutput IsAssignableFrom<TAssertionBuilder, TOutput, TActual, TExpected, TAnd, TOr>(this TAssertionBuilder assertionBuilder) 
        where TExpected : TActual
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
        where TOutput : InvokableAssertionBuilder<TActual, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<TActual, TAnd, TOr>, IOutputsChain<TOutput, TActual>
    {
        return new DelegateAssertCondition<TActual, TExpected, TAnd, TOr>(
            assertionBuilder.AppendCallerMethod(typeof(TExpected).FullName),
            default,
            (value, _, _, _) => value!.GetType().IsAssignableFrom(typeof(TExpected)),
            (actual, _) => $"{actual?.GetType()} is not assignable from {typeof(TExpected).Name}")
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder); }
}