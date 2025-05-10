namespace TUnit.Assertions.AssertConditions;

public abstract class ConvertToAssertCondition<TFromType, TToType> : BaseAssertCondition<TFromType>
{
    public abstract ValueTask<(AssertionResult, TToType?)> ConvertValue(TFromType? value);
    
    public TToType? ConvertedValue { get; private set; }

    protected override sealed async ValueTask<AssertionResult> GetResult(TFromType? actualValue, Exception? exception, AssertionMetadata assertionMetadata)
    {
        var (result, convertedValue) = await ConvertValue(actualValue);
        
        ConvertedValue = convertedValue;
        
        return result;
    }
}