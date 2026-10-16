namespace TUnit.Core.Exceptions;

public class AfterTestFilteringException(string message, Exception innerException) : TUnitException(message, innerException);
