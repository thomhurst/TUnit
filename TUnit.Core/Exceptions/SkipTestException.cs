namespace TUnit.Core.Exceptions;

public class SkipTestException : TUnitException
{
    public string Reason { get; }

    public SkipTestException(string reason)
    {
        Reason = reason;
    }
}