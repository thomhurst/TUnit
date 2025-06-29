using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;

namespace TUnit.Assertions.Assertions.Generics.Conditions;

public class ValueConversionAssertionCondition<TFromType, TToType>(
    ISource source,
    ConvertToAssertCondition<TFromType, TToType> convertToAssertCondition) : BaseAssertCondition<TToType>
{
    internal protected override string GetExpectation() => convertToAssertCondition.GetExpectation();

    protected override async ValueTask<AssertionResult> GetResult(
        TToType? actualValue, Exception? exception,
        AssertionMetadata assertionMetadata
    )
    {
        return await convertToAssertCondition.GetAssertionResult(await source.AssertionDataTask);
    }
}
