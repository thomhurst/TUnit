namespace TUnit.Core.Exceptions;

public class FailTestException : TUnitException
{
    public string Reason { get; }

    public FailTestException(string reason)
    {
        Reason = reason;
    }
}