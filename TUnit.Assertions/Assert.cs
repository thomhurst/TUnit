using TUnit.Assertions.Exceptions;

namespace TUnit.Assertions;

public static class Assert
{
    public static void That<T>(T value, AssertCondition<T> assertCondition)
    {
        if (!assertCondition.Matches(value))
        {
            var message = GetMessage(assertCondition, value);
            throw new AssertionException(message);
        }
    }

    private static string GetMessage<T>(AssertCondition<T> assertCondition, T actualValue)
    {
        return assertCondition.MessageFactory != null 
            ? assertCondition.MessageFactory((assertCondition.ExpectedValue, actualValue)) 
            : assertCondition.Message;
    }
}