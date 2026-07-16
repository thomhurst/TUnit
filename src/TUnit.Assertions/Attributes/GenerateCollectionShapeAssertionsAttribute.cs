using System;

namespace TUnit.Assertions.Attributes;

/// <summary>
/// Marks a generic, arity-1 assertion source (one whose single type parameter is the wrapped value type and
/// which exposes a public <c>AssertionContext&lt;T&gt; Context</c>) as wanting the full collection-shape-specific
/// assertion surface generated as forwarding extension methods — one overload set per collection shape
/// (IEnumerable / IReadOnlyList / IList / List / array / set / dictionary / …).
///
/// <para>
/// The generator (<c>CollectionShapeAssertionGenerator</c>) reflects the real public method surface of each
/// shape's assertion source, so per-shape signature differences (e.g. the <c>IEqualityComparer</c> parameter
/// on <c>ReadOnlyList.HasItemAt</c> that <c>IList.HasItemAt</c> lacks) are always correct and new methods are
/// picked up automatically. See issue #6185.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
internal sealed class GenerateCollectionShapeAssertionsAttribute : Attribute
{
}
