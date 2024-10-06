using TUnit.Assertions.AssertConditions.String;

namespace TUnit.Assertions.AssertionBuilders.Wrappers;

public class StringContainsAssertionBuilderWrapper : InvokableValueAssertionBuilder<string>
{
    internal StringContainsAssertionBuilderWrapper(InvokableAssertionBuilder<string> invokableAssertionBuilder) : base(invokableAssertionBuilder)
    {
    }

    public StringContainsAssertionBuilderWrapper WithTrimming()
    {
        var assertion = (StringEqualsAssertCondition) Assertions.Peek();

        assertion.Trimmed();
        
        return this;
    }
    
    public StringContainsAssertionBuilderWrapper IgnoringWhitespace()
    {
        var assertion = (StringEqualsAssertCondition) Assertions.Peek();

        assertion.IgnoringWhitespace();
        
        return this;
    }
}