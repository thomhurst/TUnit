namespace TUnit.Core.Exceptions;

public class InconclusiveTestException(string message, Exception exception) : TUnitException(message, exception);