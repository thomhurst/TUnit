using TUnit.Core.Interfaces;

namespace TUnit.Core;

public record NotInParallelConstraint(IReadOnlyList<string> NotInParallelConstraintKeys) : IParallelConstraint
{
    public int Order { get; set; } = int.MaxValue / 2;
}
