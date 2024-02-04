namespace TUnit.Assertions.Exceptions;

public class AssertionException : TUnitException
{
    public AssertionException(string? message) : base(message)
    {
    }
}