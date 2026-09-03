namespace TUnit.Core.Exceptions;

public class TimeoutException : TUnitException
{
    internal TimeoutException(TimeSpan timeSpan) : base(GetMessage(timeSpan))
    {
    }

    internal TimeoutException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    private static string GetMessage(TimeSpan timeSpan)
    {
        return $"The test timed out after {timeSpan.TotalMilliseconds} milliseconds";
    }
}
