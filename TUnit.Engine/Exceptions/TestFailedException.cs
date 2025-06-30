namespace TUnit.Engine.Exceptions;

public class TestFailedException : TUnitFailedException
{
    public TestFailedException(Exception exception) : base(exception)
    {
    }

    public TestFailedException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
