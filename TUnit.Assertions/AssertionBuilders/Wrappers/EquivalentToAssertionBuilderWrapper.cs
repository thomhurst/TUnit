using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using TUnit.Assertions.Assertions.Generics.Conditions;
using TUnit.Assertions.Enums;

namespace TUnit.Assertions.AssertionBuilders.Wrappers;

public class EquivalentToAssertionBuilderWrapper<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
TActual,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
TExpected> : InvokableValueAssertionBuilder<TActual>
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

    public EquivalentToAssertionBuilderWrapper<TActual, TExpected> IgnoringMembersOfType<T>()
    {
        var assertion = (EquivalentToExpectedValueAssertCondition<TActual, TExpected>) Assertions.Peek();

        assertion.IgnoringMembersOfType<T>();

        return this;
    }

    public EquivalentToAssertionBuilderWrapper<TActual, TExpected> IgnoringMembersOfType(Type type)
    {
        var assertion = (EquivalentToExpectedValueAssertCondition<TActual, TExpected>) Assertions.Peek();

        assertion.IgnoringMembersOfType(type);

        return this;
    }

    public EquivalentToAssertionBuilderWrapper<TActual, TExpected> WithPartialEquivalency()
    {
        var assertion = (EquivalentToExpectedValueAssertCondition<TActual, TExpected>) Assertions.Peek();

        assertion.EquivalencyKind = EquivalencyKind.Partial;

        return this;
    }
}
