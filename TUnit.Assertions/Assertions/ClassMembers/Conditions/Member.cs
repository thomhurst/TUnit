using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.AssertConditions.ClassMember;

public class Member<TActualRootType, TPropertyType>(IValueSource<TActualRootType> valueSource, Expression<Func<TActualRootType, TPropertyType>> selector)
{
    [RequiresUnreferencedCode("Expression compilation requires unreferenced code")]
    [RequiresDynamicCode("Expression compilation requires dynamic code generation")]
    public InvokableValueAssertionBuilder<TActualRootType> EqualTo(TPropertyType expected, [CallerArgumentExpression(nameof(expected))] string? doNotPopulateThisValue = null)
    {
        return valueSource.RegisterAssertion(new PropertyEqualsExpectedValueAssertCondition<TActualRootType, TPropertyType>(selector, expected, true)
            , [doNotPopulateThisValue]);
    }

    [RequiresUnreferencedCode("Expression compilation requires unreferenced code")]
    [RequiresDynamicCode("Expression compilation requires dynamic code generation")]
    public InvokableValueAssertionBuilder<TActualRootType> NotEqualTo(TPropertyType expected, [CallerArgumentExpression(nameof(expected))] string? doNotPopulateThisValue = null)
    {
        return valueSource.RegisterAssertion(new PropertyEqualsExpectedValueAssertCondition<TActualRootType, TPropertyType>(selector, expected, false)
            , [doNotPopulateThisValue]);
    }
}
