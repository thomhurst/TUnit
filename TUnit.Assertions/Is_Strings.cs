using TUnit.Assertions.AssertConditions.String;

namespace TUnit.Assertions;

public static partial class Is
{
    public static AssertCondition<string> EqualTo(string expected, StringComparison stringComparison = StringComparison.Ordinal)
    {
        return new StringEqualsAssertCondition(expected, stringComparison);
    }
}