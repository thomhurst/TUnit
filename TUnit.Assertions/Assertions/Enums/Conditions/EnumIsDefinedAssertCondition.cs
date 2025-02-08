using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.Assertions.Enums.Conditions;

public class EnumIsDefinedAssertCondition<TEnum> : BaseAssertCondition<TEnum> where TEnum : Enum
{
    protected override string GetExpectation()
    {
        return "to be defined";
    }

    protected override Task<AssertionResult> GetResult(TEnum? actualValue, Exception? exception,
        AssertionMetadata assertionMetadata)
    {
        return AssertionResult.FailIf(actualValue is null, "the source enum is null")
            .OrFailIf(!Enum.IsDefined(typeof(TEnum), actualValue!), "it was not");
    }
}