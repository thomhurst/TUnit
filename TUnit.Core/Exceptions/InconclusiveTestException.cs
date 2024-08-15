namespace TUnit.Core.Exceptions;

public class InconclusiveTestException : TUnitException
{
    public InconclusiveTestException(string message, Exception exception) : base(message, exception)
    {
    }
}