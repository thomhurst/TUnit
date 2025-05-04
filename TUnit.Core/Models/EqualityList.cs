using System.Collections;

namespace TUnit.Core;

public class EqualityList<T>(IList<T> backingList) : IList<T>
{
    private readonly IList<T> _backingList = backingList;

    public EqualityList() : this([])
    {
    }

    public IEnumerator<T> GetEnumerator()
    {
        return _backingList.GetEnumerator();
    }

    public override bool Equals(object? obj)
    {
        return obj is IEnumerable<T> other && Equals(other);
    }

    protected bool Equals(EqualityList<T> other)
    {
        return _backingList.SequenceEqual(other);
    }

    public override int GetHashCode()
    {
        return _backingList.GetHashCode();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)_backingList).GetEnumerator();
    }

    public static implicit operator EqualityList<T>(List<T> list) => new(list);
    public static implicit operator EqualityList<T>(T[] list) => new(list);
    public static implicit operator T[](EqualityList<T> equalityList)
    {
        if (equalityList._backingList is T[] array)
        {
            return array;
        }
        
        return equalityList._backingList.ToArray();
    }

    public void Add(T item)
    {
        _backingList.Add(item);
    }

    public void Clear()
    {
        _backingList.Clear();
    }

    public bool Contains(T item)
    {
        return _backingList.Contains(item);
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        _backingList.CopyTo(array, arrayIndex);
    }

    public bool Remove(T item)
    {
        return _backingList.Remove(item);
    }

    public int Count => _backingList.Count;
    public bool IsReadOnly => _backingList.IsReadOnly;

    public int IndexOf(T item)
    {
        return _backingList.IndexOf(item);
    }

    public void Insert(int index, T item)
    {
        _backingList.Insert(index, item);
    }

    public void RemoveAt(int index)
    {
        _backingList.RemoveAt(index);
    }

    public T this[int index]
    {
        get => _backingList[index];
        set => _backingList[index] = value;
    }
}