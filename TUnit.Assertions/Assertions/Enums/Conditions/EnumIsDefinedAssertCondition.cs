using System;
using System.Threading.Tasks;
using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.Assertions.Enums.Conditions;

public class EnumIsDefinedAssertCondition<TEnum> : BaseAssertCondition<TEnum>
    where TEnum : struct, Enum
{
    internal protected override string GetExpectation() => "to be defined";

    protected override ValueTask<AssertionResult> GetResult(
        TEnum actualValue, Exception? exception,
        AssertionMetadata assertionMetadata
    )
    {
        return AssertionResult.FailIf(
            !Enum.IsDefined(typeof(TEnum), actualValue), "the value is not defined in the enum");
    }
}

public class EnumIsNotDefinedAssertCondition<TEnum> : BaseAssertCondition<TEnum>
    where TEnum : struct, Enum
{
    internal protected override string GetExpectation() => "to not be defined";

    protected override ValueTask<AssertionResult> GetResult(
        TEnum actualValue, Exception? exception,
        AssertionMetadata assertionMetadata
    )
    {
        return AssertionResult.FailIf(
            Enum.IsDefined(typeof(TEnum), actualValue), "the value is defined in the enum");
    }
}