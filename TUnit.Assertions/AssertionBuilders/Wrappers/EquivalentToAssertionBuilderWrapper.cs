using System.Runtime.CompilerServices;
using TUnit.Assertions.Assertions.Generics.Conditions;
using TUnit.Assertions.Enums;

namespace TUnit.Assertions.AssertionBuilders.Wrappers;

public class EquivalentToAssertionBuilderWrapper<TActual, TExpected> : InvokableValueAssertionBuilder<TActual>
{
    internal EquivalentToAssertionBuilderWrapper(InvokableAssertionBuilder<TActual> invokableAssertionBuilder) : base(invokableAssertionBuilder)
    {
    }

    public EquivalentToAssertionBuilderWrapper<TActual, TExpected> IgnoringMember(string propertyName, [CallerArgumentExpression(nameof(propertyName))] string doNotPopulateThis = "")
    {
        var assertion = (EquivalentToExpectedValueAssertCondition<TActual, TExpected>) Assertions.Peek();

        assertion.IgnoringMember(propertyName);

        AppendCallerMethod([doNotPopulateThis]);

        return this;
    }

    public EquivalentToAssertionBuilderWrapper<TActual, TExpected> IgnoringType<TType>()
    {
        var assertion = (EquivalentToExpectedValueAssertCondition<TActual, TExpected>) Assertions.Peek();

        assertion.IgnoringType(typeof(TType));

        AppendCallerMethod([$"<{typeof(TType).Name}>"]);

        return this;
    }

    public EquivalentToAssertionBuilderWrapper<TActual, TExpected> IgnoringType(Type type, [CallerArgumentExpression(nameof(type))] string doNotPopulateThis = "")
    {
        var assertion = (EquivalentToExpectedValueAssertCondition<TActual, TExpected>) Assertions.Peek();

        assertion.IgnoringType(type);

        AppendCallerMethod([doNotPopulateThis]);

        return this;
    }

    public EquivalentToAssertionBuilderWrapper<TActual, TExpected> WithPartialEquivalency()
    {
        var assertion = (EquivalentToExpectedValueAssertCondition<TActual, TExpected>) Assertions.Peek();

        assertion.EquivalencyKind = EquivalencyKind.Partial;

        return this;
    }
}
