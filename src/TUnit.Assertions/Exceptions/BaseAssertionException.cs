namespace TUnit.Assertions.Exceptions;

public class BaseAssertionException : Exception
{
    public BaseAssertionException()
    {
    }

    public BaseAssertionException(string? message) : base(message)
    {
    }

    public BaseAssertionException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
