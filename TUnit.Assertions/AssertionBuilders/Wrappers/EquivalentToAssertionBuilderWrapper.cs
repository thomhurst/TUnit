using System.Runtime.CompilerServices;
using TUnit.Assertions.Assertions.Generics.Conditions;

namespace TUnit.Assertions.AssertionBuilders.Wrappers;

public class EquivalentToAssertionBuilderWrapper<TActual> : InvokableValueAssertionBuilder<TActual>
{
    internal EquivalentToAssertionBuilderWrapper(InvokableAssertionBuilder<TActual> invokableAssertionBuilder) : base(invokableAssertionBuilder)
    {
    }
    
    public EquivalentToAssertionBuilderWrapper<TActual> IgnoringMember(string propertyName, [CallerArgumentExpression(nameof(propertyName))] string doNotPopulateThis = "")
    {
        var assertion = (EquivalentToExpectedValueAssertCondition<TActual>) Assertions.Peek();

        assertion.IgnoringMember(propertyName);
        
        AppendCallerMethod([doNotPopulateThis]);
        
        return this;
    }
}