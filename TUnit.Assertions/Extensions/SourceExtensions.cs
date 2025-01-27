using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static class SourceExtensions
{
    public static InvokableValueAssertionBuilder<TActual> RegisterAssertion<TActual>(this IValueSource<TActual> source,
        BaseAssertCondition<TActual> assertCondition, string[] argumentExpressions, [CallerMemberName] string caller = "")
    {
        return new InvokableValueAssertionBuilder<TActual>(assertCondition.ChainedTo(source.AssertionBuilder, argumentExpressions, caller));
    }
    
    public static InvokableValueAssertionBuilder<TActual> RegisterAssertionWithoutExpression<TActual>(this IValueSource<TActual> source,
        BaseAssertCondition<TActual> assertCondition)
    {
        return new InvokableValueAssertionBuilder<TActual>((InvokableValueAssertionBuilder<TActual>) source.WithAssertion(assertCondition));
    }
    
    public static InvokableDelegateAssertionBuilder<TActual> RegisterAssertion<TActual>(this IDelegateSource<TActual> source,
        BaseAssertCondition<TActual> assertCondition, string[] argumentExpressions, [CallerMemberName] string caller = "")
    {
        return new InvokableDelegateAssertionBuilder<TActual>(assertCondition.ChainedTo(source.AssertionBuilder, argumentExpressions, caller));
    }
    
    public static InvokableDelegateAssertionBuilder<TActual> RegisterAssertionWithoutExpression<TActual>(this IDelegateSource<TActual> source,
        BaseAssertCondition<TActual> assertCondition)
    {
        return new InvokableDelegateAssertionBuilder<TActual>(assertCondition.ChainedToWithoutExpression(source.AssertionBuilder));
    }
}