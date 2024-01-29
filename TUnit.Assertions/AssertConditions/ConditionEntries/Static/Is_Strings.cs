using TUnit.Assertions.AssertConditions.String;

namespace TUnit.Assertions.AssertConditions.ConditionEntries.Static;

public static partial class Is
{
    public static AssertCondition<string> EqualTo(string expected)
    {
        return new StringEqualsAssertCondition(expected, StringComparison.Ordinal);
    }
    
    internal static AssertCondition<string> EqualTo(IReadOnlyCollection<ExpectedValueAssertCondition<string, string>> previousConditions, string expected)
    {
        return new StringEqualsAssertCondition(previousConditions, expected, StringComparison.Ordinal);
    }
    
    public static AssertCondition<string> EqualTo(string expected, StringComparison stringComparison)
    {
        return new StringEqualsAssertCondition(expected, stringComparison);
    }
    
    internal static AssertCondition<string> EqualTo(IReadOnlyCollection<ExpectedValueAssertCondition<string, string>> previousConditions, string expected, StringComparison stringComparison)
    {
        return new StringEqualsAssertCondition(previousConditions, expected, stringComparison);
    }
}