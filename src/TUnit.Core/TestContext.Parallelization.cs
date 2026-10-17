using TUnit.Core.Enums;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

public partial class TestContext
{
    internal IReadOnlyList<IParallelConstraint> ParallelConstraints => _parallelConstraints == null
        ? Array.Empty<IParallelConstraint>()
        : _parallelConstraints;

    internal Priority ExecutionPriority { get; set; } = Priority.Normal;

    /// <summary>
    /// Rank from the last <c>OrderTestsBy</c> call of the stage that ran this test (tests with equal keys
    /// share a rank); <see langword="null"/> when no hook ordered it. Written when the stage is sealed.
    /// The scheduler sorts by it ahead of its class-name and <c>[NotInParallel(Order)]</c> keys, so those
    /// still order tests within a rank; unranked tests sort before ranked ones.
    /// </summary>
    internal int? HookRank { get; set; }

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

    internal void ClearParallelConstraints()
    {
        _parallelConstraints?.Clear();
    }
}
