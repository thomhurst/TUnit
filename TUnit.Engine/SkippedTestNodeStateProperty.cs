using Microsoft.Testing.Platform.Extensions.Messages;

namespace TUnit.Engine;

/// <summary>
/// Skipped test state property
/// </summary>
public class SkippedTestNodeStateProperty : IProperty
{
    public string Reason { get; }
    
    public SkippedTestNodeStateProperty(string reason)
    {
        Reason = reason;
    }
}