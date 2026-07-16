using TUnit.Core.Enums;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Interfaces;

/// <summary>
/// Provides access to test parallelization control and configuration.
/// Accessed via <see cref="TestContext.Parallelism"/>.
/// </summary>
public interface ITestParallelization
{
    /// <summary>
    /// Gets the collection of parallel constraints applied to this test.
    /// Multiple constraints can be combined (e.g., ParallelGroup + NotInParallel).
    /// </summary>
    IReadOnlyList<IParallelConstraint> Constraints { get; }

    /// <summary>
    /// Gets or sets the execution priority for this test.
    /// Higher priority tests may execute before lower priority tests when resources are limited.
    /// </summary>
    Priority ExecutionPriority { get; set; }

    /// <summary>
    /// Gets the parallel limiter that controls how many tests can run concurrently.
    /// </summary>
    IParallelLimit? Limiter { get; }

    /// <summary>
    /// Adds a parallel constraint to this test context.
    /// Multiple constraints can be combined to create complex parallelization rules.
    /// </summary>
    /// <param name="constraint">The constraint to add</param>
    void AddConstraint(IParallelConstraint constraint);
}
