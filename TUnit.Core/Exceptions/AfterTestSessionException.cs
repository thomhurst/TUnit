namespace TUnit.Core.Exceptions;

public class AfterTestSessionException(string message, Exception innerException) : TUnitException(message, innerException);
