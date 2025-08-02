namespace TUnit.Assertions.Exceptions;

public class AssertionException : BaseAssertionException
{
    public AssertionException(string? message) : base(message)
    {
    }

    public AssertionException(string? message, Exception innerException) : base(message, innerException)
    {
    }
}
