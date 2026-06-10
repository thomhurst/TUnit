using System;

namespace TUnit.Assertions.Attributes;

/// <summary>
/// Marks the core generic "item-source" forwarding method (e.g. <c>Satisfies&lt;TSource&gt;</c>) so the
/// generator emits one per-collection-shape overload that binds the most specific assertion source
/// (CollectionAssertion / ListAssertion / SetAssertion / DictionaryAssertion / …) for the user lambda.
/// Replaces the hand-written per-shape block in <c>CollectionItemSatisfiesExtensions.cs</c>. See issue #6185.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class GenerateCollectionShapeItemSourceOverloadsAttribute : Attribute
{
}
