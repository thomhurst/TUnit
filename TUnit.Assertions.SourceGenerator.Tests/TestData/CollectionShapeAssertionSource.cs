using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.SourceGenerator.Tests.TestData;

// Minimal arity-1 wrapper source used to exercise CollectionShapeAssertionGenerator (Emitter A).
// The generator reflects the real shape-assertion-source method surface from the referenced
// TUnit.Assertions assembly, so per-shape signature fidelity (e.g. the ReadOnlyList HasItemAt
// comparer parameter that IList lacks) is asserted against the actual symbols. Only the single
// type parameter and the public Context property are required by the generator.
[GenerateCollectionShapeAssertions]
public sealed class CollectionShapeAssertionSource<T>
{
    public AssertionContext<T> Context { get; } = null!;
}
