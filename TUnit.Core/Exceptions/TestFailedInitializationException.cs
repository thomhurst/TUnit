namespace TUnit.Core.Exceptions;

public class TestFailedInitializationException : Exception
{
    public TestFailedInitializationException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}