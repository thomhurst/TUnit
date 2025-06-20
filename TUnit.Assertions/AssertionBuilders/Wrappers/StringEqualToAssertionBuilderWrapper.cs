using TUnit.Assertions.AssertConditions.String;

namespace TUnit.Assertions.AssertionBuilders.Wrappers;

public class StringEqualToAssertionBuilderWrapper : InvokableValueAssertionBuilder<string>
{
    internal StringEqualToAssertionBuilderWrapper(InvokableAssertionBuilder<string> invokableAssertionBuilder) : base(invokableAssertionBuilder)
    {
    }

    public StringEqualToAssertionBuilderWrapper WithTrimming()
    {
        var assertion = (StringEqualsExpectedValueAssertCondition) Assertions.Peek();

        assertion.WithTrimming();
        
        AppendCallerMethod([]);
        
        return this;
    }
    
    public StringEqualToAssertionBuilderWrapper WithNullAndEmptyEquality()
    {
        var assertion = (StringEqualsExpectedValueAssertCondition) Assertions.Peek();

        assertion.WithNullAndEmptyEquality();
        
        AppendCallerMethod([]);
        
        return this;
    }
    
    public StringEqualToAssertionBuilderWrapper IgnoringWhitespace()
    {
        var assertion = (StringEqualsExpectedValueAssertCondition) Assertions.Peek();

        assertion.IgnoringWhitespace();
        
        AppendCallerMethod([]);
        
        return this;
    }
    

}