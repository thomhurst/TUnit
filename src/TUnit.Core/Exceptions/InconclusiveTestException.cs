namespace TUnit.Core.Exceptions;

public class InconclusiveTestException : TUnitException
{
    public InconclusiveTestException(string message) : base(message)
    {
    }

    public InconclusiveTestException(string message, Exception exception) : base(message, exception)
    {
    }
}
