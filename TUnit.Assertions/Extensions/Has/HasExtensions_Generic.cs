using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.ClassMember;
using TUnit.Assertions.AssertConditions.Operators;
using TUnit.Assertions.AssertionBuilders;

namespace TUnit.Assertions.Extensions;

public static partial class HasExtensions
{
    public static Member<TRootObject, TPropertyType, TAnd, TOr> HasMember<TRootObject, TPropertyType, TAnd, TOr>(this AssertionBuilder<TRootObject, TAnd, TOr> assertionBuilder, Expression<Func<TRootObject, TPropertyType>> selector, [CallerArgumentExpression("selector")] string expression = "")
        where TAnd : IAnd<TRootObject, TAnd, TOr>
        where TOr : IOr<TRootObject, TAnd, TOr> => new(assertionBuilder.AppendCallerMethod(expression), selector);
}