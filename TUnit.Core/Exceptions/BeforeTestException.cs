namespace TUnit.Core.Exceptions;

public class BeforeTestException(string message, Exception innerException) : TUnitException(message, innerException);
