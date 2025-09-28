using System;
using TUnit.Assertions.Assertions.Base;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.AssertConditions.String;
using TUnit.Assertions.AssertionBuilders;
using TUnit.Assertions.AssertionBuilders.Interfaces;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// String equality assertion
/// </summary>
public class StringEqualToAssertion : Assertion<string>
{
    private readonly string _expected;
    private readonly StringComparison _stringComparison;
    private bool _trim = false;
    private bool _nullAndEmptyEqual = false;
    private bool _ignoreWhitespace = false;

    internal StringEqualToAssertion(IValueSource<string> source, string expected, 
        StringComparison stringComparison, IAssertionChain chain = null!)
        : base(source, chain)
    {
        _expected = expected;
        _stringComparison = stringComparison;
    }

    public StringEqualToAssertion WithTrimming()
    {
        _trim = true;
        return this;
    }

    public StringEqualToAssertion WithNullAndEmptyEquality()
    {
        _nullAndEmptyEqual = true;
        return this;
    }

    public StringEqualToAssertion IgnoringWhitespace()
    {
        _ignoreWhitespace = true;
        return this;
    }

    protected override BaseAssertCondition CreateCondition()
    {
        var condition = new StringEqualsExpectedValueAssertCondition(_expected, _stringComparison);
        
        if (_trim)
            condition.WithTrimming();
        if (_nullAndEmptyEqual)
            condition.WithNullAndEmptyEquality();
        if (_ignoreWhitespace)
            condition.IgnoringWhitespace();
        
        return condition;
    }
    
    // Override And and Or to return the correct type
    public new StringEqualToAssertion And 
    { 
        get 
        {
            _ = base.And;
            return this;
        }
    }
    
    public new StringEqualToAssertion Or
    {
        get
        {
            _ = base.Or;
            return this;
        }
    }
    
    public new StringEqualToAssertion Because(string reason, string? expression = null)
    {
        base.Because(reason, expression);
        return this;
    }
}