namespace TUnit.Core.Interfaces;

/// <summary>
/// Interface for generating random values of type T for property-based testing
/// </summary>
/// <typeparam name="T">The type of value to generate</typeparam>
public interface IGenerator<out T>
{
    /// <summary>
    /// Generate a random value using the provided random number generator
    /// </summary>
    /// <param name="random">Random number generator to use for generation</param>
    /// <returns>A randomly generated value of type T</returns>
    T Generate(Random random);
}
