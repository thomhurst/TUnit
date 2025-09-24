using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using TUnit.Assertions.Assertions.Generics.Conditions;
using TUnit.Assertions.Enums;

namespace TUnit.Assertions.AssertionBuilders.Wrappers;

public class EquivalentToAssertionBuilderWrapper<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
    TActual,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
    TExpected> : AssertionBuilderWrapperBase<TActual>
{
    internal EquivalentToAssertionBuilderWrapper(AssertionBuilder<TActual> invokableAssertionBuilder)
        : base(invokableAssertionBuilder)
    {
    }

    public EquivalentToAssertionBuilderWrapper<TActual, TExpected> IgnoringMember(string propertyName, [CallerArgumentExpression(nameof(propertyName))] string doNotPopulateThis = "")
    {
        var assertion = GetLastAssertionAs<EquivalentToExpectedValueAssertCondition<TActual, TExpected>>();

        assertion.IgnoringMember(propertyName);

        AppendCallerMethod([doNotPopulateThis]);

        return this;
    }

    public EquivalentToAssertionBuilderWrapper<TActual, TExpected> IgnoringType<TType>()
    {
        var assertion = GetLastAssertionAs<EquivalentToExpectedValueAssertCondition<TActual, TExpected>>();

        assertion.IgnoringType(typeof(TType));

        AppendCallerMethod([$"<{typeof(TType).Name}>"]);

        return this;
    }

    public EquivalentToAssertionBuilderWrapper<TActual, TExpected> IgnoringType(Type type, [CallerArgumentExpression(nameof(type))] string doNotPopulateThis = "")
    {
        var assertion = GetLastAssertionAs<EquivalentToExpectedValueAssertCondition<TActual, TExpected>>();

        assertion.IgnoringType(type);

        AppendCallerMethod([doNotPopulateThis]);

        return this;
    }

    public EquivalentToAssertionBuilderWrapper<TActual, TExpected> WithPartialEquivalency()
    {
        var assertion = GetLastAssertionAs<EquivalentToExpectedValueAssertCondition<TActual, TExpected>>();

        assertion.EquivalencyKind = EquivalencyKind.Partial;

        return this;
    }
}
