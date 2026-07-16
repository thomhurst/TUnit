namespace TUnit.Core.Exceptions;

public class BeforeTestSessionException(string message, Exception innerException) : TUnitException(message, innerException);
