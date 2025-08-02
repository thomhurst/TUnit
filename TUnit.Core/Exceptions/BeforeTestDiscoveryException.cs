namespace TUnit.Core.Exceptions;

public class BeforeTestDiscoveryException(string message, Exception innerException) : TUnitException(message, innerException);
