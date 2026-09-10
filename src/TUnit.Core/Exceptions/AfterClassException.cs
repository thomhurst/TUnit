namespace TUnit.Core.Exceptions;

public class AfterClassException(string message, Exception innerException) : TUnitException(message, innerException);
