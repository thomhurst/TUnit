using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.Assertions.Base;
using TUnit.Assertions.Assertions.Generics.Conditions;
using TUnit.Assertions.Enums;
using TUnit.Assertions.Extensions;
using TUnit.Assertions.AssertionBuilders.Interfaces;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// Clean not-equivalent assertion - no inheritance, just configuration
/// </summary>
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public class NotEquivalentToAssertion<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
    TActual,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
    TExpected> : Assertion<TActual>
{
    private readonly TExpected _expected;
    private readonly string? _expectedExpression;
    private readonly string?[] _expressions;
    
    // Configuration
    private readonly List<string> _ignoredMembers = new();
    private readonly List<Type> _ignoredTypes = new();
    private EquivalencyKind _equivalencyKind = EquivalencyKind.Full;

    internal NotEquivalentToAssertion(IValueSource<TActual> source, TExpected expected, 
        string? expectedExpression, string?[] expressions, IAssertionChain chain = null!)
        : base(source, chain)
    {
        _expected = expected;
        _expectedExpression = expectedExpression;
        _expressions = expressions;
    }

    public NotEquivalentToAssertion<TActual, TExpected> IgnoringMember(string propertyName, 
        [CallerArgumentExpression(nameof(propertyName))] string doNotPopulateThis = "")
    {
        _ignoredMembers.Add(propertyName);
        return this;
    }

    public NotEquivalentToAssertion<TActual, TExpected> IgnoringType<TType>()
    {
        _ignoredTypes.Add(typeof(TType));
        return this;
    }

    public NotEquivalentToAssertion<TActual, TExpected> IgnoringType(Type type, 
        [CallerArgumentExpression(nameof(type))] string doNotPopulateThis = "")
    {
        _ignoredTypes.Add(type);
        return this;
    }

    public NotEquivalentToAssertion<TActual, TExpected> WithPartialEquivalency()
    {
        _equivalencyKind = EquivalencyKind.Partial;
        return this;
    }

    protected override BaseAssertCondition? CreateCondition()
    {
        var condition = new NotEquivalentToExpectedValueAssertCondition<TActual, TExpected>(_expected, _expectedExpression);
        
        foreach (var member in _ignoredMembers)
            condition.IgnoringMember(member);
        
        foreach (var type in _ignoredTypes)
            condition.IgnoringType(type);
        
        condition.EquivalencyKind = _equivalencyKind;
        
        return condition;
    }
}
