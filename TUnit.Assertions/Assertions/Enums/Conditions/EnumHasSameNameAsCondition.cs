using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.Assertions.Enums.Conditions;

public class EnumHasSameNameAsCondition<TEnum, TExpected>(TExpected expected) : BaseAssertCondition<TEnum> 
    where TEnum : Enum
    where TExpected : Enum
{
    protected override string GetExpectation()
    {
        return $"to have the same name as {Enum.GetName(typeof(TExpected), expected)}";
    }

    protected override Task<AssertionResult> GetResult(TEnum? actualValue, Exception? exception,
        AssertionMetadata assertionMetadata)
    {
        return AssertionResult.FailIf(actualValue is null, "the source enum is null")
            .OrFailIf(Enum.GetName(typeof(TEnum), actualValue!) != Enum.GetName(typeof(TExpected), expected), $"the name was {Enum.GetName(typeof(TEnum), actualValue!)}");
    }
}