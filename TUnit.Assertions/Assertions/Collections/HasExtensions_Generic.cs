using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.ClassMember;
using TUnit.Assertions.AssertConditions.Interfaces;

namespace TUnit.Assertions.Extensions;

public static partial class HasExtensions
{
    public static Member<TRootObject, TPropertyType> HasMember<TRootObject, TPropertyType>(this IValueSource<TRootObject> valueSource, Expression<Func<TRootObject, TPropertyType>> selector, [CallerArgumentExpression(nameof(selector))] string expression = "")
    {
        valueSource.AssertionBuilder.AppendCallerMethod([expression]);
        return new(valueSource, selector);
    }
}