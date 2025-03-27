using TUnit.Assertions.AssertConditions;

namespace TUnit.Assertions.Assertions.Generics.Conditions;

public class ConvertExceptionToValueAssertCondition<TException> : BaseAssertCondition<object?>
    where TException : Exception
{
    protected override string GetExpectation()
    {
        return $"to throw {typeof(TException).Name}";
    }
    
    public TException? ConvertedExceptionValue { get; private set; }

    protected sealed override ValueTask<AssertionResult> GetResult(object? actualValue, Exception? exception, AssertionMetadata assertionMetadata)
    {
        if (exception is null)
        {
            return FailWithMessage("No exception was thrown");
        }
        
        if (exception is not TException castException)
        {
            return FailWithMessage($"Expected {typeof(TException).Name} but received {exception.GetType().Name}");
        }
        
        ConvertedExceptionValue = castException;
        
        return AssertionResult.Passed;
    }
}