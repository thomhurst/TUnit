using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.ClassMember;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static partial class HasExtensions
{
    public static Member<TAssertionBuilder, TOutput, TRootObject, TPropertyType, TAnd, TOr> HasMember<TAssertionBuilder, TOutput, TRootObject, TPropertyType, TAnd, TOr>(this TAssertionBuilder assertionBuilder, Expression<Func<TRootObject, TPropertyType>> selector, [CallerArgumentExpression("selector")] string expression = "")
        where TAnd : IAnd<TRootObject, TAnd, TOr>
        where TOr : IOr<TRootObject, TAnd, TOr>
        where TAssertionBuilder : AssertionBuilder<TRootObject, TAnd, TOr>, IOutputsChain<TOutput, TRootObject>
        where TOutput : InvokableAssertionBuilder<TRootObject, TAnd, TOr> => new((TAssertionBuilder)assertionBuilder.AppendCallerMethod(expression), selector);
}