namespace TUnit.Core.Exceptions;

public class BeforeAssemblyException(string message, Exception innerException) : TUnitException(message, innerException);
