#nullable disable

using System.Collections;
using TUnit.Assertions.AssertConditions.Collections;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static partial class HasExtensions
{
    public static TOutput HasSingleItem<TAssertionBuilder, TOutput, TActual, TAnd, TOr>(this TAssertionBuilder assertionBuilder, IEqualityComparer equalityComparer = null) 
        where TActual : IEnumerable
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
        where TOutput : InvokableAssertionBuilder<TActual, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<TActual, TAnd, TOr>, IOutputsChain<TOutput, TActual>
    {
        return new EnumerableCountEqualToAssertCondition<TActual, TAnd, TOr>(
            assertionBuilder.AppendCallerMethod(null),
            1)
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder);
    }
    
    public static TOutput HasDistinctItems<TAssertionBuilder, TOutput, TActual, TAnd, TOr>(this TAssertionBuilder assertionBuilder) 
        where TActual : IEnumerable
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
        where TOutput : InvokableAssertionBuilder<TActual, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<TActual, TAnd, TOr>, IOutputsChain<TOutput, TActual>
    {
        return new EnumerableDistinctItemsAssertCondition<TActual, object, TAnd, TOr>(
            assertionBuilder.AppendCallerMethod(null),
            default,
            null)
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder);
    }
    
    public static TOutput HasDistinctItems<TAssertionBuilder, TOutput, TActual, TInner, TAnd, TOr>(this TAssertionBuilder assertionBuilder, IEqualityComparer<TInner> equalityComparer) 
        where TActual : IEnumerable<TInner>
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
        where TOutput : InvokableAssertionBuilder<TActual, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<TActual, TAnd, TOr>, IOutputsChain<TOutput, TActual>
    {
        return new EnumerableDistinctItemsAssertCondition<TActual, TInner, TAnd, TOr>(
            assertionBuilder.AppendCallerMethod(null),
            default,
            equalityComparer)
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder);
    }
    
    public static EnumerableCount<TAssertionBuilder, TOutput, TActual, TAnd, TOr> HasCount<TAssertionBuilder, TOutput, TActual, TAnd, TOr>(this TAssertionBuilder assertionBuilder) 
        where TActual : IEnumerable
        where TAnd : IAnd<TActual, TAnd, TOr>
        where TOr : IOr<TActual, TAnd, TOr> 
        where TOutput : InvokableAssertionBuilder<TActual, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<TActual, TAnd, TOr>, IOutputsChain<TOutput, TActual>
    {
        return new EnumerableCount<TAssertionBuilder, TOutput, TActual, TAnd, TOr>((TAssertionBuilder)assertionBuilder.AppendCallerMethod(null));
    }
}