namespace TUnit.Core.Exceptions;

public class BeforeClassException(string message, Exception innerException) : TUnitException(message, innerException);
