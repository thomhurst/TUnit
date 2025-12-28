namespace TUnit.FsCheck;

/// <summary>
/// Exception thrown when an FsCheck property test fails.
/// </summary>
public class PropertyFailedException : Exception
{
    public PropertyFailedException(string message) : base(message)
    {
    }

    public PropertyFailedException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
