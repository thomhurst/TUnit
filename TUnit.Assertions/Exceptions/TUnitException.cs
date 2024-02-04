using System.Runtime.Serialization;

namespace TUnit.Assertions.Exceptions;

public class TUnitException : Exception
{
    public TUnitException()
    {
    }

    protected TUnitException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public TUnitException(string? message) : base(message)
    {
    }

    public TUnitException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}