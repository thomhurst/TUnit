using Microsoft.Testing.Platform.Extensions.Messages;

namespace TUnit.Engine;

/// <summary>
/// Duration property for test execution time
/// </summary>
public class DurationProperty : IProperty
{
    public TimeSpan Duration { get; }
    
    public DurationProperty(TimeSpan duration)
    {
        Duration = duration;
    }
}