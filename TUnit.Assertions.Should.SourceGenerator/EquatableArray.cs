using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace TUnit.Assertions.Should.SourceGenerator;

/// <summary>
/// Wraps <see cref="ImmutableArray{T}"/> with structural equality so source generator
/// pipeline values participate in incremental caching. Without this, reference-equality
/// on the underlying arrays defeats <see cref="Microsoft.CodeAnalysis.IncrementalValueProvider{TValue}"/>
/// caching and re-emits every output on every keystroke.
/// </summary>
internal readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IEnumerable<T>
    where T : IEquatable<T>
{
    private readonly ImmutableArray<T> _array;

    public EquatableArray(ImmutableArray<T> array) => _array = array;

    public int Length => _array.IsDefault ? 0 : _array.Length;

    public T this[int index] => _array[index];

    public bool Equals(EquatableArray<T> other)
    {
        if (_array.IsDefault) return other._array.IsDefault;
        if (other._array.IsDefault) return false;
        return _array.AsSpan().SequenceEqual(other._array.AsSpan());
    }

    public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

    public override int GetHashCode()
    {
        if (_array.IsDefault) return 0;
        var hash = 17;
        foreach (var item in _array)
        {
            hash = unchecked(hash * 31 + (item?.GetHashCode() ?? 0));
        }
        return hash;
    }

    public IEnumerator<T> GetEnumerator() =>
        _array.IsDefault ? Enumerable.Empty<T>().GetEnumerator() : ((IEnumerable<T>)_array).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public static implicit operator EquatableArray<T>(ImmutableArray<T> array) => new(array);
}
