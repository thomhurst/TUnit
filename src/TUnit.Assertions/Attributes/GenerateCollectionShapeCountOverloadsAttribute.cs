using System;

namespace TUnit.Assertions.Attributes;

/// <summary>
/// Marks the shared <c>Count(itemAssertion)</c> specialisation helper so the generator emits one
/// per-collection-shape overload of <c>Count</c> whose item-assertion lambda receives the most specific
/// assertion source for each item shape. Replaces the hand-written per-shape block in
/// <c>AssertionExtensions.cs</c> (#5707). See issue #6185.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
internal sealed class GenerateCollectionShapeCountOverloadsAttribute : Attribute
{
}
