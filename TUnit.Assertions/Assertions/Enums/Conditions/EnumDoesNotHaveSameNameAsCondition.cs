using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.Assertions.Enums.Conditions;

public class EnumDoesNotHaveSameNameAsCondition<TEnum, TExpected>(TExpected expected) : BaseAssertCondition<TEnum>
    where TEnum : Enum
    where TExpected : Enum
{
    internal protected override string GetExpectation()
    {
        return $"to not have the same name as {Enum.GetName(typeof(TExpected), expected)}";
    }

    protected override ValueTask<AssertionResult> GetResult(
        TEnum? actualValue, Exception? exception,
        AssertionMetadata assertionMetadata
    )
    {
        return AssertionResult.FailIf(actualValue is null, "the source enum is null")
            .OrFailIf(actualValue is not null && Enum.GetName(typeof(TEnum), actualValue) == Enum.GetName(typeof(TExpected), expected), "the name was the same");
    }
}
