using System.Runtime.CompilerServices;
using TUnit.Assertions.Assertions.Generics.Conditions;
using TUnit.Assertions.Enums;

namespace TUnit.Assertions.AssertionBuilders.Wrappers;

public class NotEquivalentToAssertionBuilderWrapper<TActual, TExpected> : InvokableValueAssertionBuilder<TActual>
{
    internal NotEquivalentToAssertionBuilderWrapper(InvokableAssertionBuilder<TActual> invokableAssertionBuilder) : base(invokableAssertionBuilder)
    {
    }
    
    public NotEquivalentToAssertionBuilderWrapper<TActual, TExpected> IgnoringMember(string propertyName, [CallerArgumentExpression(nameof(propertyName))] string doNotPopulateThis = "")
    {
        var assertion = (NotEquivalentToExpectedValueAssertCondition<TActual, TExpected>) Assertions.Peek();

        assertion.IgnoringMember(propertyName);
        
        AppendCallerMethod([doNotPopulateThis]);
        
        return this;
    }

    public NotEquivalentToAssertionBuilderWrapper<TActual, TExpected> WithPartialEquivalency()
    {
        var assertion = (NotEquivalentToExpectedValueAssertCondition<TActual, TExpected>) Assertions.Peek();

        assertion.EquivalencyKind = EquivalencyKind.Partial;

        return this;
    }
}