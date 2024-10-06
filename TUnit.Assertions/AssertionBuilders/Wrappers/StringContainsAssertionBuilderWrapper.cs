using TUnit.Assertions.AssertConditions.String;

namespace TUnit.Assertions.AssertionBuilders.Wrappers;

public class StringContainsAssertionBuilderWrapper : InvokableValueAssertionBuilder<string>
{
    internal StringContainsAssertionBuilderWrapper(InvokableAssertionBuilder<string> invokableAssertionBuilder) : base(invokableAssertionBuilder)
    {
    }

    public StringContainsAssertionBuilderWrapper WithTrimming()
    {
        var assertion = (StringEqualsExpectedValueAssertCondition) Assertions.Peek();

        assertion.WithTransform(s => s.Trim(), s => s.Trim());
        
        return this;
    }
    
    public StringContainsAssertionBuilderWrapper IgnoringWhitespace()
    {
        var assertion = (StringEqualsExpectedValueAssertCondition) Assertions.Peek();

        assertion.WithTransform(StringUtils.StripWhitespace, StringUtils.StripWhitespace);
        
        return this;
    }
}