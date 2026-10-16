namespace TUnit.Core.Exceptions;

public class BeforeTestFilteringException(string message, Exception innerException) : TUnitException(message, innerException);
