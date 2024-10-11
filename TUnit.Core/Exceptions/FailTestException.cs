namespace TUnit.Core.Exceptions;

public class FailTestException(string reason) : TUnitException
{
    public string Reason { get; } = reason;
}