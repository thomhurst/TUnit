using System.Collections;

namespace TUnit.Core.Helpers;

// Retains a tuple parameter's boundary and its mutable element boxes until invocation.
// A boxed ValueTuple would produce fresh struct copies whenever its elements are read.
internal sealed class TupleArgument(Type tupleType, object?[] elements) : IReadOnlyList<object?>
{
    public Type TupleType { get; } = tupleType;
    public object?[] Elements { get; } = elements;
    public int Count => Elements.Length;
    public object? this[int index] => Elements[index];
    public IEnumerator<object?> GetEnumerator() => ((IEnumerable<object?>)Elements).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
