using TUnit.Core.Interfaces;

namespace TUnit.Core;

public record ParallelGroupConstraint(string Group) : IParallelConstraint;