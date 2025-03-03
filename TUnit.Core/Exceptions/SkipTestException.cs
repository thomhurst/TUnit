namespace TUnit.Core.Exceptions;

public class SkipTestException(string reason) : TUnitException(reason);