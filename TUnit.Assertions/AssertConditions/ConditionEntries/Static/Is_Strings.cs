using TUnit.Assertions.AssertConditions.String;

namespace TUnit.Assertions.AssertConditions.ConditionEntries.Static;

public static partial class Is
{
    public static AssertCondition<string> EqualTo(string expected)
    {
        return new StringEqualsAssertCondition(expected, StringComparison.Ordinal);
    }
    
    public static AssertCondition<string> EqualTo(string expected, StringComparison stringComparison)
    {
        return new StringEqualsAssertCondition(expected, stringComparison);
    }
}