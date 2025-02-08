using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.Assertions.Enums.Conditions;

public class EnumDoesNotHaveSameValueAsCondition<TEnum, TExpected>(TExpected expected) : BaseAssertCondition<TEnum> 
    where TEnum : Enum
    where TExpected : Enum
{
    protected override string GetExpectation()
    {
        return $"to not have the same value as {Enum.GetName(typeof(TExpected), expected)}";
    }

    protected override Task<AssertionResult> GetResult(TEnum? actualValue, Exception? exception,
        AssertionMetadata assertionMetadata)
    {
        return AssertionResult.FailIf(actualValue is null, "the source enum is null")
            .OrFailIf(Convert.ToInt32(actualValue!) == Convert.ToInt32(expected), "the value was the same");
    }
}