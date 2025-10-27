namespace TUnit.Core.Interfaces;

/// <summary>
/// Interface for shrinking values of type T to simpler/smaller values for property-based testing
/// </summary>
/// <typeparam name="T">The type of value to shrink</typeparam>
public interface IShrinker<T>
{
    /// <summary>
    /// Generate a sequence of "smaller" or "simpler" values from the given value.
    /// Each yielded value should be strictly "smaller" than the input to ensure termination.
    /// </summary>
    /// <param name="value">The value to shrink</param>
    /// <returns>An enumerable sequence of simpler values</returns>
    IEnumerable<T> Shrink(T value);
}
