using Microsoft.Testing.Platform.Extensions.Messages;

namespace TUnit.Engine;

/// <summary>
/// Timing property
/// </summary>
public class TimingProperty : IProperty
{
    public TimeSpan Duration { get; }
    
    public TimingProperty(TimeSpan duration)
    {
        Duration = duration;
    }
}