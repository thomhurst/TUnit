namespace TUnit.Core.Exceptions;

public class AfterAssemblyException(string message, Exception innerException) : TUnitException(message, innerException);
