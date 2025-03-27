using TUnit.Assertions.AssertConditions;
using TUnit.Assertions.AssertConditions.Interfaces;

namespace TUnit.Assertions.Assertions.Generics.Conditions;

public class DelegateConversionAssertionCondition<TToType>(
    IDelegateSource source,
    ConvertExceptionToValueAssertCondition<TToType> convertToAssertCondition) : BaseAssertCondition<TToType> where TToType : Exception
{
    protected override string GetExpectation() => convertToAssertCondition.Expectation;

    protected override async ValueTask<AssertionResult> GetResult(
        TToType? actualValue, Exception? exception,
        AssertionMetadata assertionMetadata
    )
    {
        return await convertToAssertCondition.GetAssertionResult(await source.AssertionDataTask);
    }
}