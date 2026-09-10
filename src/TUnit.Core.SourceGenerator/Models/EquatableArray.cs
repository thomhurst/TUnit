using System.Collections;

namespace TUnit.Core.SourceGenerator.Models;

/// <summary>
/// A wrapper around arrays that provides value equality semantics.
/// Essential for incremental source generators where proper caching depends on equality comparison.
/// </summary>
/// <typeparam name="T">The element type, which must implement IEquatable&lt;T&gt;</typeparam>
public readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IEnumerable<T>
    where T : IEquatable<T>
{
    private readonly T[]? _array;

    public EquatableArray(T[]? array) => _array = array;

    public EquatableArray(IEnumerable<T>? items) => _array = items?.ToArray();

    public static EquatableArray<T> Empty => new(Array.Empty<T>());

    public T[] AsArray() => _array ?? Array.Empty<T>();

    public int Length => _array?.Length ?? 0;

    public T this[int index] => (_array ?? Array.Empty<T>())[index];

    public bool Equals(EquatableArray<T> other)
    {
        var left = _array ?? Array.Empty<T>();
        var right = other._array ?? Array.Empty<T>();

        if (left.Length != right.Length)
        {
            return false;
        }

        for (var i = 0; i < left.Length; i++)
        {
            if (!left[i].Equals(right[i]))
            {
                return false;
            }
        }

        return true;
    }

    public override bool Equals(object? obj)
    {
        return obj is EquatableArray<T> other && Equals(other);
    }

    public override int GetHashCode()
    {
        var array = _array ?? Array.Empty<T>();

        unchecked
        {
            var hash = 17;
            foreach (var item in array)
            {
                hash = hash * 31 + (item?.GetHashCode() ?? 0);
            }
            return hash;
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        return ((IEnumerable<T>)(_array ?? Array.Empty<T>())).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right)
    {
        return !left.Equals(right);
    }

    public static implicit operator EquatableArray<T>(T[]? array) => new(array);

    public static implicit operator T[](EquatableArray<T> array) => array.AsArray();
}

/// <summary>
/// Extension methods for creating EquatableArray instances.
/// </summary>
public static class EquatableArrayExtensions
{
    public static EquatableArray<T> ToEquatableArray<T>(this IEnumerable<T>? items)
        where T : IEquatable<T>
    {
        return new EquatableArray<T>(items);
    }

    public static EquatableArray<T> ToEquatableArray<T>(this T[]? array)
        where T : IEquatable<T>
    {
        return new EquatableArray<T>(array);
    }
}
