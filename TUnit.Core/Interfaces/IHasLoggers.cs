using TUnit.Core.Logging;

namespace TUnit.Core.Interfaces;

/// <summary>
/// Defines a contract for components that maintain a collection of loggers.
/// </summary>
public interface IHasLoggers
{
    /// <summary>
    /// Gets the collection of loggers available for logging operations.
    /// </summary>
    /// <remarks>
    /// The collection contains <see cref="TUnitLogger"/> instances that can be used
    /// to record messages at various log levels throughout the testing pipeline. These loggers
    /// provide both synchronous and asynchronous logging capabilities.
    /// </remarks>
    /// <value>
    /// A <see cref="List{T}"/> of <see cref="TUnitLogger"/> 
    /// instances configured for the component.
    /// </value>
    public List<TUnitLogger> Loggers { get; }
}