namespace TUnit.Engine.Diagnostics;

/// <summary>
/// Exception thrown when Debug.Assert or Trace.Assert fails
/// </summary>
public class TUnitAssertionFailedException : Exception
{
    public TUnitAssertionFailedException(string message) : base(message)
    {
    }
    
    public TUnitAssertionFailedException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}