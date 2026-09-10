using TUnit.Assertions.Core;

// Local stand-in for the (internal) TUnit.Assertions.Attributes.GenerateCollectionShapeAssertionsAttribute.
// The generator matches the trigger by metadata name, so a same-named source declaration drives it without
// needing friend access to the real internal attribute (which strong-naming would otherwise require). The
// real attribute is invisible here (internal, no InternalsVisibleTo), so there is no accessible conflict.
namespace TUnit.Assertions.Attributes
{
    [System.AttributeUsage(System.AttributeTargets.Class)]
    internal sealed class GenerateCollectionShapeAssertionsAttribute : System.Attribute;
}

namespace TUnit.Assertions.SourceGenerator.Tests.TestData
{
    // Minimal arity-1 wrapper source used to exercise CollectionShapeAssertionGenerator (Emitter A).
    // The generator reflects the real shape-assertion-source method surface from the referenced
    // TUnit.Assertions assembly, so per-shape signature fidelity (e.g. the ReadOnlyList HasItemAt
    // comparer parameter that IList lacks) is asserted against the actual symbols. Only the single
    // type parameter and the public Context property are required by the generator.
    [TUnit.Assertions.Attributes.GenerateCollectionShapeAssertions]
    public sealed class CollectionShapeAssertionSource<T>
    {
        public AssertionContext<T> Context { get; } = null!;
    }
}
