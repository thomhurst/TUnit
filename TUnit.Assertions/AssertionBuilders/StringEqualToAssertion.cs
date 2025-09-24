using System.Linq;
using System.Runtime.CompilerServices;
using TUnit.Assertions.AssertConditions.String;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// Fluent assertion builder for string equality comparisons
/// </summary>
public class StringEqualToAssertion : FluentAssertionBase<string, StringEqualToAssertion>
{
    internal StringEqualToAssertion(AssertionBuilder<string> assertionBuilder) 
        : base(assertionBuilder)
    {
    }

    public StringEqualToAssertion WithTrimming()
    {
        var assertions = GetAssertions();
        var assertion = assertions.LastOrDefault() as StringEqualsExpectedValueAssertCondition;
        
        if (assertion != null)
        {
            assertion?.WithTrimming();
            AppendExpression("WithTrimming()");
        }

        return Self;
    }

    public StringEqualToAssertion WithNullAndEmptyEquality()
    {
        var assertions = GetAssertions();
        var assertion = assertions.LastOrDefault() as StringEqualsExpectedValueAssertCondition;
        
        if (assertion != null)
        {
            assertion?.WithNullAndEmptyEquality();
            AppendExpression("WithNullAndEmptyEquality()");
        }

        return Self;
    }

    public StringEqualToAssertion IgnoringWhitespace()
    {
        var assertions = GetAssertions();
        var assertion = assertions.LastOrDefault() as StringEqualsExpectedValueAssertCondition;
        
        if (assertion != null)
        {
            assertion?.IgnoringWhitespace();
            AppendExpression("IgnoringWhitespace()");
        }

        return Self;
    }
}