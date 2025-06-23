using TUnit.Core.Models;

namespace TUnit.Core.Interfaces;

/// <summary>
/// Expands test metadata into executable test variations.
/// This interface provides a unified API that works in both source generation and reflection modes.
/// </summary>
public interface ITestVariationExpander
{
    /// <summary>
    /// Expands a test descriptor into all possible test variations.
    /// </summary>
    /// <param name="testDescriptor">The test descriptor to expand</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>All test variations for the descriptor</returns>
    IAsyncEnumerable<TestVariation> ExpandTestVariationsAsync(
        ITestDescriptor testDescriptor, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the estimated number of variations for a test descriptor without full expansion.
    /// Used for progress reporting and resource allocation.
    /// </summary>
    /// <param name="testDescriptor">The test descriptor</param>
    /// <returns>Estimated number of variations</returns>
    Task<int> EstimateVariationCountAsync(ITestDescriptor testDescriptor);
}