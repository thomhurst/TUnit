using Microsoft.Testing.Platform.Extensions.Messages;

namespace TUnit.Engine;

/// <summary>
/// Timeout test state property
/// </summary>
public class TimeoutTestNodeStateProperty : IProperty
{
    public string Message { get; }
    
    public TimeoutTestNodeStateProperty(string message)
    {
        Message = message;
    }
}