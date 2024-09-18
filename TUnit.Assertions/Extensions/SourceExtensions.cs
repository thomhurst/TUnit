using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static class SourceExtensions
{
    public static InvokableAssertionBuilder<TActual, TAnd, TOr> RegisterAssertion<TActual, TAnd, TOr>(this ISource<TActual, TAnd, TOr> source,
        BaseAssertCondition<TActual> assertCondition, string[] argumentExpressions, [CallerMemberName] string caller = "") 
        where TAnd : IAnd<TActual, TAnd, TOr> 
        where TOr : IOr<TActual, TAnd, TOr>
    {
        return assertCondition.ChainedTo(source.AssertionBuilder, argumentExpressions, caller);
    }
}