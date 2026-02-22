using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace TUnit.Mocks.SourceGenerator.Models;

/// <summary>
/// An equatable wrapper around ImmutableArray that provides structural equality.
/// Required for IIncrementalGenerator pipeline caching to work correctly.
/// </summary>
internal readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IEnumerable<T>
    where T : IEquatable<T>
{
    private readonly ImmutableArray<T> _array;

    public EquatableArray(ImmutableArray<T> array) => _array = array;

    public static EquatableArray<T> Empty => new(ImmutableArray<T>.Empty);

    public int Length => _array.IsDefault ? 0 : _array.Length;

    public T this[int index] => _array[index];

    public bool Equals(EquatableArray<T> other)
    {
        if (_array.IsDefault && other._array.IsDefault) return true;
        if (_array.IsDefault || other._array.IsDefault) return false;
        if (_array.Length != other._array.Length) return false;

        for (int i = 0; i < _array.Length; i++)
        {
            if (!_array[i].Equals(other._array[i]))
                return false;
        }
        return true;
    }

    public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

    public override int GetHashCode()
    {
        if (_array.IsDefault) return 0;

        unchecked
        {
            int hash = 17;
            foreach (var item in _array)
                hash = hash * 31 + item.GetHashCode();
            return hash;
        }
    }

    public ImmutableArray<T> AsImmutableArray() => _array.IsDefault ? ImmutableArray<T>.Empty : _array;

    public IEnumerator<T> GetEnumerator()
    {
        if (_array.IsDefault) return ((IEnumerable<T>)ImmutableArray<T>.Empty).GetEnumerator();
        return ((IEnumerable<T>)_array).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right) => left.Equals(right);
    public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right) => !left.Equals(right);
}
