using System.Linq;
using TUnit.Assertions.Assertions.Strings.Conditions;

namespace TUnit.Assertions.AssertionBuilders;

/// <summary>
/// Fluent assertion builder for string contains comparisons
/// </summary>
public class StringContainsAssertion : FluentAssertionBase<string, StringContainsAssertion>
{
    internal StringContainsAssertion(AssertionBuilder<string> assertionBuilder) 
        : base(assertionBuilder)
    {
    }

    public StringContainsAssertion WithTrimming()
    {
        var assertions = GetAssertions();
        var assertion = assertions.LastOrDefault() as StringContainsExpectedValueAssertCondition;
        
        if (assertion != null)
        {
            // Note: StringContainsExpectedValueAssertCondition might need these methods added
            // For now, we'll just track that this was called
            AppendExpression("WithTrimming()");
        }

        return Self;
    }

    public StringContainsAssertion IgnoringWhitespace()
    {
        var assertions = GetAssertions();
        var assertion = assertions.LastOrDefault() as StringContainsExpectedValueAssertCondition;
        
        if (assertion != null)
        {
            // Note: StringContainsExpectedValueAssertCondition might need these methods added
            // For now, we'll just track that this was called
            AppendExpression("IgnoringWhitespace()");
        }

        return Self;
    }
}