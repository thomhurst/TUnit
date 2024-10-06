using TUnit.Assertions.AssertConditions.String;

namespace TUnit.Assertions.AssertionBuilders.Wrappers;

public class StringEqualToAssertionBuilderWrapper : InvokableValueAssertionBuilder<string>
{
    internal StringEqualToAssertionBuilderWrapper(InvokableAssertionBuilder<string> invokableAssertionBuilder) : base(invokableAssertionBuilder)
    {
    }

    public StringEqualToAssertionBuilderWrapper WithTrimming()
    {
        var assertion = (StringEqualsAssertCondition) Assertions.Peek();

        assertion.Trimmed();
        
        return this;
    }
    
    public StringEqualToAssertionBuilderWrapper WithNullAndEmptyEquality()
    {
        var assertion = (StringEqualsAssertCondition) Assertions.Peek();

        assertion.WithNullAndEmptyEquality();
        
        return this;
    }
    
    public StringEqualToAssertionBuilderWrapper IgnoringWhitespace()
    {
        var assertion = (StringEqualsAssertCondition) Assertions.Peek();

        assertion.IgnoringWhitespace();
        
        return this;
    }
}