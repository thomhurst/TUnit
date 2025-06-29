namespace TUnit.Core.Exceptions;

public class TUnitException : Exception
{
    public TUnitException()
    {
    }

    public TUnitException(string? message) : base(message)
    {
    }

    public TUnitException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
