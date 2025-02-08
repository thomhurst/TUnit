using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.Assertions.Enums.Conditions;

public class EnumDoesNotHaveFlagAssertCondition<TEnum>(TEnum expected) : BaseAssertCondition<TEnum> where TEnum : Enum
{
    protected override string GetExpectation()
    {
        return $"to not have the flag {expected.ToString()}";
    }

    protected override Task<AssertionResult> GetResult(TEnum? actualValue, Exception? exception,
        AssertionMetadata assertionMetadata)
    {
        return AssertionResult.FailIf(actualValue is null, "the source enum is null")
            .OrFailIf(actualValue!.HasFlag(expected), "the flag was present");
    }
}