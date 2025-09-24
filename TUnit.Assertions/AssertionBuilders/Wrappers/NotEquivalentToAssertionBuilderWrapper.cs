using System.Runtime.CompilerServices;
using TUnit.Assertions.Assertions.Generics.Conditions;
using TUnit.Assertions.Enums;

namespace TUnit.Assertions.AssertionBuilders.Wrappers;

public class NotEquivalentToAssertionBuilderWrapper<TActual, TExpected> : AssertionBuilderWrapperBase<TActual>
{
    internal NotEquivalentToAssertionBuilderWrapper(AssertionBuilder<TActual> invokableAssertionBuilder)
        : base(invokableAssertionBuilder)
    {
    }

    public NotEquivalentToAssertionBuilderWrapper<TActual, TExpected> IgnoringMember(string propertyName, [CallerArgumentExpression(nameof(propertyName))] string doNotPopulateThis = "")
    {
        var assertion = GetLastAssertionAs<NotEquivalentToExpectedValueAssertCondition<TActual, TExpected>>();

        assertion.IgnoringMember(propertyName);

        AppendCallerMethod([doNotPopulateThis]);

        return this;
    }

    public NotEquivalentToAssertionBuilderWrapper<TActual, TExpected> IgnoringType<TType>()
    {
        var assertion = GetLastAssertionAs<NotEquivalentToExpectedValueAssertCondition<TActual, TExpected>>();

        assertion.IgnoringType(typeof(TType));

        AppendCallerMethod([$"<{typeof(TType).Name}>"]);

        return this;
    }

    public NotEquivalentToAssertionBuilderWrapper<TActual, TExpected> IgnoringType(Type type, [CallerArgumentExpression(nameof(type))] string doNotPopulateThis = "")
    {
        var assertion = GetLastAssertionAs<NotEquivalentToExpectedValueAssertCondition<TActual, TExpected>>();

        assertion.IgnoringType(type);

        AppendCallerMethod([doNotPopulateThis]);

        return this;
    }

    public NotEquivalentToAssertionBuilderWrapper<TActual, TExpected> WithPartialEquivalency()
    {
        var assertion = GetLastAssertionAs<NotEquivalentToExpectedValueAssertCondition<TActual, TExpected>>();

        assertion.EquivalencyKind = EquivalencyKind.Partial;

        return this;
    }
}
