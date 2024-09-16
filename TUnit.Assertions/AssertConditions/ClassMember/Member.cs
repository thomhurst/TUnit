using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.AssertConditions.ClassMember;

public class Member<TAssertionBuilder, TOutput, TActualRootType, TPropertyType, TAnd, TOr>(TAssertionBuilder assertionBuilder, Expression<Func<TActualRootType, TPropertyType>> selector)
    where TAnd : IAnd<TActualRootType, TAnd, TOr>
    where TOr : IOr<TActualRootType, TAnd, TOr>
    where TOutput : InvokableAssertionBuilder<TActualRootType, TAnd, TOr>
    where TAssertionBuilder : AssertionBuilder<TActualRootType, TAnd, TOr>, IOutputsChain<TOutput, TActualRootType>
{
    public TOutput EqualTo(TPropertyType expected, [CallerArgumentExpression("expected")] string doNotPopulateThisValue = "")
    {
        return new PropertyEqualsAssertCondition<TActualRootType, TPropertyType, TAnd, TOr>(assertionBuilder.AppendCallerMethod(doNotPopulateThisValue), selector, expected, true)
            .ChainedTo<TAssertionBuilder, TOutput, TAnd, TOr>(assertionBuilder);
    }

    public NotMember<TAssertionBuilder, TOutput, TActualRootType, TPropertyType, TAnd, TOr> Not => new(assertionBuilder, selector);
}