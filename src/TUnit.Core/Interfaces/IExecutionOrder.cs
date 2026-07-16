namespace TUnit.Core.Interfaces;

/// <summary>
/// Defines the execution order for tests
/// </summary>
public interface IExecutionOrder
{
    /// <summary>
    /// Gets the priority value. Higher values execute first.
    /// </summary>
    int Priority { get; }
    
    /// <summary>
    /// Gets the order within the same priority. Lower values execute first.
    /// </summary>
    int Order { get; }
}