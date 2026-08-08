namespace TUnit.Core.Exceptions;

public class FailTestException(string reason) : TUnitException(reason)
{
    public string Reason { get; } = reason;
}
