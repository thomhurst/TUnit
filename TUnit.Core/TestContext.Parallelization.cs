using TUnit.Core.Enums;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Test parallelization control and configuration
/// Implements <see cref="ITestParallelization"/> interface
/// </summary>
public partial class TestContext
{
    // Explicit interface implementations for ITestParallelization
    IReadOnlyList<IParallelConstraint> ITestParallelization.Constraints => ParallelConstraints;
    Priority ITestParallelization.ExecutionPriority
    {
        get => ExecutionPriority;
        set => ExecutionPriority = value;
    }
    IParallelLimit? ITestParallelization.Limiter => ParallelLimiter;

    void ITestParallelization.AddConstraint(IParallelConstraint constraint)
    {
        if (constraint != null)
        {
            _parallelConstraints.Add(constraint);
        }
    }

    void ITestParallelization.SetLimiter(IParallelLimit parallelLimit)
    {
        ParallelLimiter = parallelLimit;
    }
}
