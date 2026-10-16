namespace TUnit.Core.Exceptions;

/// <summary>
/// Thrown when a <c>[After(TestFiltering)]</c> hook fails; the hook's exception is the inner exception.
/// </summary>
public class AfterTestFilteringException(string message, Exception innerException) : TUnitException(message, innerException);
