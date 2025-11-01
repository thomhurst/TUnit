using TUnit.Core.Enums;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

public partial class TestContext
{
    internal IReadOnlyList<IParallelConstraint> ParallelConstraints => _parallelConstraints ?? [];
    internal Priority ExecutionPriority { get; set; } = Priority.Normal;

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
            _parallelConstraints ??= [];
            _parallelConstraints.Add(constraint);
        }
    }

    void ITestParallelization.SetLimiter(IParallelLimit parallelLimit)
    {
        ParallelLimiter = parallelLimit;
    }
}
