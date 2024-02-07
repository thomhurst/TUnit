namespace TUnit.Assertions.Exceptions;

public class AssertionException : TUnitException
{
    public AssertionException(string? message) : base(message)
    {
    }
    
    public AssertionException(string? message, Exception innerException) : base(message, innerException)
    {
    }
}