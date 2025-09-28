using System;
using TUnit.Assertions.Assertions.Base;
using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;
using TUnit.Assertions.Assertions.Strings.Conditions;
using TUnit.Assertions.AssertionBuilders.Core;
using TUnit.Assertions.AssertionBuilders.Interfaces;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// String contains assertion
/// </summary>
public class StringContainsAssertion : Assertion<string>
{
    private readonly string _expected;
    private readonly StringComparison _stringComparison;
    // private bool _withTrimming; // TODO: Implement when available in the condition
    private bool _ignoringWhitespace;

    internal StringContainsAssertion(IValueSource<string> source, string expected, 
        StringComparison stringComparison, IAssertionChain chain = null!)
        : base(source, chain)
    {
        _expected = expected;
        _stringComparison = stringComparison;
    }

    public StringContainsAssertion WithTrimming()
    {
        // _withTrimming = true; // TODO: Implement when available in the condition
        return this;
    }

    public StringContainsAssertion IgnoringWhitespace()
    {
        _ignoringWhitespace = true;
        return this;
    }

    protected override BaseAssertCondition CreateCondition()
    {
        var condition = new StringContainsExpectedValueAssertCondition(_expected, _stringComparison);
        
        if (_ignoringWhitespace)
            condition.IgnoreWhitespace = true;
        
        // TODO: Add support for WithTrimming when available in the condition
        
        return condition;
    }
    
    // Override And and Or to return the correct type
    public new StringContainsAssertion And 
    { 
        get 
        {
            _ = base.And;
            return this;
        }
    }
    
    public new StringContainsAssertion Or
    {
        get
        {
            _ = base.Or;
            return this;
        }
    }
    
    public new StringContainsAssertion Because(string reason, string? expression = null)
    {
        base.Because(reason, expression);
        return this;
    }
}
