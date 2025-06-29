using Microsoft.Testing.Platform.Extensions.Messages;

namespace TUnit.Engine;

/// <summary>
/// Failed test state property
/// </summary>
public class FailedTestNodeStateProperty : IProperty
{
    public Exception Exception { get; }
    
    public FailedTestNodeStateProperty(Exception exception)
    {
        Exception = exception;
    }
}