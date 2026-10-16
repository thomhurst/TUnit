namespace TUnit.Core.Exceptions;

/// <summary>
/// Thrown when a <c>[Before(TestFiltering)]</c> hook fails; the hook's exception is the inner exception.
/// </summary>
public class BeforeTestFilteringException(string message, Exception innerException) : TUnitException(message, innerException);
