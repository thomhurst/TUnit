using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.Assertions.Generics.Conditions;
using TUnit.Assertions.Enums;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// Fluent assertion builder for not-equivalent comparisons
/// </summary>
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public class NotEquivalentToAssertion<TActual> : FluentAssertionBase<TActual, NotEquivalentToAssertion<TActual>>
{
    internal NotEquivalentToAssertion(AssertionBuilder<TActual> assertionBuilder)
        : base(assertionBuilder)
    {
    }

    public NotEquivalentToAssertion<TActual> IgnoringMember(string propertyName, [CallerArgumentExpression(nameof(propertyName))] string doNotPopulateThis = "")
    {
        var assertion = GetLastAssertionAs<IEquivalentCondition>();
        assertion?.IgnoringMember(propertyName);

        AppendCallerMethod([doNotPopulateThis]);
        return this;
    }

    public NotEquivalentToAssertion<TActual> IgnoringType<TType>()
    {
        var assertion = GetLastAssertionAs<IEquivalentCondition>();
        assertion?.IgnoringType(typeof(TType));

        AppendCallerMethod([$"<{typeof(TType).Name}>"]);
        return this;
    }

    public NotEquivalentToAssertion<TActual> IgnoringType(Type type, [CallerArgumentExpression(nameof(type))] string doNotPopulateThis = "")
    {
        var assertion = GetLastAssertionAs<IEquivalentCondition>();
        assertion?.IgnoringType(type);

        AppendCallerMethod([doNotPopulateThis]);
        return this;
    }

    public NotEquivalentToAssertion<TActual> WithPartialEquivalency()
    {
        var assertion = GetLastAssertionAs<IEquivalentCondition>();
        if (assertion != null)
        {
            assertion.EquivalencyKind = EquivalencyKind.Partial;
        }

        AppendCallerMethod([]);
        return this;
    }
}