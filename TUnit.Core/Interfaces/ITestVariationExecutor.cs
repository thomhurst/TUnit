using TUnit.Core.Models;

namespace TUnit.Core.Interfaces;

/// <summary>
/// Executes test variations in a mode-specific manner.
/// Implementations handle either source generation (AOT-safe) or reflection-based execution.
/// </summary>
public interface ITestVariationExecutor
{
    /// <summary>
    /// Creates a test class instance for the variation.
    /// </summary>
    /// <param name="variation">The test variation</param>
    /// <returns>The created test instance</returns>
    Task<object> CreateTestInstanceAsync(TestVariation variation);

    /// <summary>
    /// Invokes the test method for the variation.
    /// </summary>
    /// <param name="variation">The test variation</param>
    /// <param name="instance">The test instance</param>
    /// <returns>The result of the test method invocation</returns>
    Task<object?> InvokeTestMethodAsync(TestVariation variation, object instance);

    /// <summary>
    /// Sets property values on the test instance.
    /// </summary>
    /// <param name="variation">The test variation</param>
    /// <param name="instance">The test instance</param>
    Task SetPropertiesAsync(TestVariation variation, object instance);

    /// <summary>
    /// Gets whether this executor supports the given variation's execution mode.
    /// </summary>
    /// <param name="variation">The test variation</param>
    /// <returns>True if supported, false otherwise</returns>
    bool SupportsVariation(TestVariation variation);
}