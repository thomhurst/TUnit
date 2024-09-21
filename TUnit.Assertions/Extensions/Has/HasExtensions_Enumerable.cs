#nullable disable

using System.Collections;
using TUnit.Assertions.AssertConditions.Collections;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static partial class HasExtensions
{
    public static InvokableValueAssertionBuilder<TActual> HasSingleItem<TActual>(this IValueSource<TActual> valueSource, IEqualityComparer equalityComparer = null) 
        where TActual : IEnumerable
    {
        return valueSource.RegisterAssertion(new EnumerableCountEqualToAssertCondition<TActual>(1)
            , []);
    }
    
    public static InvokableValueAssertionBuilder<TActual> HasDistinctItems<TActual>(this IValueSource<TActual> valueSource) 
        where TActual : IEnumerable
    {
        return valueSource.RegisterAssertion(new EnumerableDistinctItemsAssertCondition<TActual, object>(default,
            null)
            , []);
    }
    
    public static InvokableValueAssertionBuilder<TActual> HasDistinctItems<TActual, TInner>(this IValueSource<TActual> valueSource, IEqualityComparer<TInner> equalityComparer) 
        where TActual : IEnumerable<TInner>
    {
        return valueSource.RegisterAssertion(new EnumerableDistinctItemsAssertCondition<TActual, TInner>(default,
            equalityComparer)
            , []);
    }
    
    public static EnumerableCount<TActual> HasCount<TActual>(this IValueSource<TActual> valueSource) 
        where TActual : IEnumerable
    {
        valueSource.AssertionBuilder.AppendCallerMethod([]);
        return new EnumerableCount<TActual>(valueSource);
    }
}