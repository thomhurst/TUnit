namespace TUnit.Assertions.AssertConditions;

public abstract class ConvertToAssertCondition<TFromType, TToType> : BaseAssertCondition<TFromType>
{
    public abstract ValueTask<(AssertionResult, TToType?)> ConvertValue(TFromType value);
    
    public TToType? ConvertedValue { get; private set; }

    protected override async ValueTask<AssertionResult> GetResult(TFromType? actualValue, Exception? exception, AssertionMetadata assertionMetadata)
    {
        if (actualValue is null)
        {
            return AssertionResult.Fail("The value was null.");
        }

        var (result, convertedValue) = await ConvertValue(actualValue);
        
        ConvertedValue = convertedValue;
        
        return result;
    }
}