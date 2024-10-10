using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.AssertionBuilders.Wrappers;

public class StringEqualToAssertionBuilderWrapper : InvokableValueAssertionBuilder<string>
{
    internal StringEqualToAssertionBuilderWrapper(InvokableAssertionBuilder<string> invokableAssertionBuilder) : base(invokableAssertionBuilder)
    {
    }

    public StringEqualToAssertionBuilderWrapper WithTrimming()
    {
        var assertion = (ExpectedValueAssertCondition<string, string>) Assertions.Peek();

        assertion.WithTransform(s => s?.Trim(), s => s?.Trim());
        
        AppendCallerMethod([]);
        
        return this;
    }
    
    public StringEqualToAssertionBuilderWrapper WithNullAndEmptyEquality()
    {
        var assertion = (ExpectedValueAssertCondition<string, string>) Assertions.Peek();

        assertion.WithComparer((actual, expected) =>
        {
            if (actual == null && expected == string.Empty)
            {
                return AssertionDecision.Pass;
            }

            if (expected == null && actual == string.Empty)
            {
                return AssertionDecision.Pass;
            }

            return AssertionDecision.Continue;
        });
        
        AppendCallerMethod([]);
        
        return this;
    }
    
    public StringEqualToAssertionBuilderWrapper IgnoringWhitespace()
    {
        var assertion = (ExpectedValueAssertCondition<string, string>) Assertions.Peek();

        assertion.WithTransform(StringUtils.StripWhitespace, StringUtils.StripWhitespace);
        
        AppendCallerMethod([]);
        
        return this;
    }
    

}