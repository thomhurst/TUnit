namespace TUnit.Core.Interfaces;

/// <summary>
/// Interface for disposing objects.
/// Follows Dependency Inversion Principle - high-level modules depend on this abstraction.
/// </summary>
public interface IDisposer
{
    /// <summary>
    /// Disposes an object asynchronously.
    /// Implementations should propagate exceptions - callers handle aggregation.
    /// </summary>
    /// <param name="obj">The object to dispose.</param>
    /// <returns>A task representing the disposal operation.</returns>
    ValueTask DisposeAsync(object? obj);
}
