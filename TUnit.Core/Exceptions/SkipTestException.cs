namespace TUnit.Core.Exceptions;

public class SkipTestException(string reason) : TUnitException
{
    public string Reason { get; } = reason;
}