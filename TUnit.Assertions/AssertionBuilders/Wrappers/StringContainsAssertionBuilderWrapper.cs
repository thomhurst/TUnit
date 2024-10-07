using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.AssertionBuilders.Wrappers;

public class StringContainsAssertionBuilderWrapper : InvokableValueAssertionBuilder<string>
{
    internal StringContainsAssertionBuilderWrapper(InvokableAssertionBuilder<string> invokableAssertionBuilder) : base(invokableAssertionBuilder)
    {
    }

    public StringContainsAssertionBuilderWrapper WithTrimming()
    {
        var assertion = (ExpectedValueAssertCondition<string, string>) Assertions.Peek();

        assertion.WithTransform(s => s?.Trim(), s => s?.Trim());

        AppendCallerMethod([]);
        
        return this;
    }
    
    public StringContainsAssertionBuilderWrapper IgnoringWhitespace()
    {
        var assertion = (ExpectedValueAssertCondition<string, string>) Assertions.Peek();

        assertion.WithTransform(StringUtils.StripWhitespace, StringUtils.StripWhitespace);
        
        AppendCallerMethod([]);
        
        return this;
    }
}