namespace TUnit.Core.Interfaces;

/// <summary>
/// Defines the base interface for all event receivers in the TUnit testing framework.
/// </summary>
/// <remarks>
/// Event receivers are components that respond to various test lifecycle events.
/// The TUnit framework uses event receivers to implement features like test skipping,
/// test configuration, and custom test behavior.
/// </remarks>
public interface IEventReceiver
{
    /// <summary>
    /// Gets the execution order of this event receiver.
    /// </summary>
    /// <remarks>
    /// Lower values run earlier in the event processing sequence. This allows prioritizing
    /// certain event receivers over others.
    /// </remarks>
    /// <value>
    /// The order value as an integer. Default is 0.
    /// </value>
#if NET
    public int Order => 0;
#else
    public int Order { get; }
#endif
}
