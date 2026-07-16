namespace TUnit.Core.Exceptions;

public class AfterTestDiscoveryException(string message, Exception innerException) : TUnitException(message, innerException);
