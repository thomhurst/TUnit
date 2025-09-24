using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.Assertions.Strings.Conditions;

namespace TUnit.Assertions.AssertionBuilders.Wrappers;

public class StringContainsAssertionBuilderWrapper : AssertionBuilderWrapperBase<string>
{
    internal StringContainsAssertionBuilderWrapper(AssertionBuilder<string> invokableAssertionBuilder)
        : base(invokableAssertionBuilder)
    {
    }

    public StringContainsAssertionBuilderWrapper WithTrimming()
    {
        var assertion = GetLastAssertionAs<ExpectedValueAssertCondition<string, string>>();

        assertion.WithTransform(s => s?.Trim(), s => s?.Trim());

        AppendCallerMethod([]);

        return this;
    }

    public StringContainsAssertionBuilderWrapper IgnoringWhitespace()
    {
        var assertion = GetLastAssertionAs<StringContainsExpectedValueAssertCondition>();

        assertion.WithTransform(StringUtils.StripWhitespace, StringUtils.StripWhitespace);
        assertion.IgnoreWhitespace = true;

        AppendCallerMethod([]);

        return this;
    }
}
