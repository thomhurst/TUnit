namespace TUnit.Core.Exceptions;

public class AfterTestException(string message, Exception innerException) : TUnitException(message, innerException);
