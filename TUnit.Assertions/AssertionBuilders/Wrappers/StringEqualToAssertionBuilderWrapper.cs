using System.Linq;
using TUnit.Assertions.AssertConditions.String;

namespace TUnit.Assertions.AssertionBuilders.Wrappers;

public class StringEqualToAssertionBuilderWrapper : AssertionBuilder<string>
{
    internal StringEqualToAssertionBuilderWrapper(AssertionBuilder<string> assertionBuilder) : base(assertionBuilder.Actual, assertionBuilder.ActualExpression)
    {
        // Copy the assertion chain from the original builder
        foreach (var assertion in assertionBuilder.GetAssertions())
        {
            WithAssertion(assertion);
        }
    }

    public StringEqualToAssertionBuilderWrapper WithTrimming()
    {
        var assertions = GetAssertions();
        var assertion = assertions.LastOrDefault() as StringEqualsExpectedValueAssertCondition;
        
        if (assertion != null)
        {
            assertion.WithTrimming();
            AppendExpression("WithTrimming()");
        }

        return this;
    }

    public StringEqualToAssertionBuilderWrapper WithNullAndEmptyEquality()
    {
        var assertions = GetAssertions();
        var assertion = assertions.LastOrDefault() as StringEqualsExpectedValueAssertCondition;
        
        if (assertion != null)
        {
            assertion.WithNullAndEmptyEquality();
            AppendExpression("WithNullAndEmptyEquality()");
        }

        return this;
    }

    public StringEqualToAssertionBuilderWrapper IgnoringWhitespace()
    {
        var assertions = GetAssertions();
        var assertion = assertions.LastOrDefault() as StringEqualsExpectedValueAssertCondition;
        
        if (assertion != null)
        {
            assertion.IgnoringWhitespace();
            AppendExpression("IgnoringWhitespace()");
        }

        return this;
    }
}
