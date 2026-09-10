namespace TUnit.Core.Exceptions;

/// <summary>
/// Exception thrown when circular test dependencies are detected.
/// </summary>
public class CircularDependencyException : Exception
{
    public CircularDependencyException() : base()
    {
    }

    public CircularDependencyException(string message) : base(message)
    {
    }

    public CircularDependencyException(string message, Exception innerException) : base(message, innerException)
    {
    }
}