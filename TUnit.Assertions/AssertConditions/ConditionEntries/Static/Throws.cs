using TUnit.Assertions.AssertConditions.Conditions;

namespace TUnit.Assertions.AssertConditions.ConditionEntries.Static;

public static class Throws
{
    public static ThrowsNothingAssertCondition Nothing => new();
    public static ThrowsNothingAsyncAssertCondition NothingAsync => new();
}