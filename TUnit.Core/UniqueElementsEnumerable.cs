using System.Collections;

namespace TUnit.Core;

public class UniqueElementsEnumerable<T> : IReadOnlyList<UniqueElementsEnumerable<T>.UniqueElementAccessor>
{
    private readonly IEnumerable<T> _enumerable;
    private List<Dictionary<int, T>> _dictionaries = [];
    private int _count;

    public UniqueElementsEnumerable(IEnumerable<T> enumerable)
    {
        _enumerable = enumerable;
        CreateNewDictionary();
    }
    
    public int Count => _count;

    public IEnumerator<UniqueElementAccessor> GetEnumerator()
    {
        for (var i = 0; i < _count; i++)
        {
            yield return this[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public UniqueElementAccessor this[int index] => new(index, this);

    public readonly struct UniqueElementAccessor
    {
        private readonly int _index;
        private readonly UniqueElementsEnumerable<T> _elementsEnumerable;

        internal UniqueElementAccessor(int index, UniqueElementsEnumerable<T> elementsEnumerable)
        {
            _index = index;
            _elementsEnumerable = elementsEnumerable;
        }

        public T Get() => _elementsEnumerable.Get(_index);
    }
    
    private T Get(int index)
    {
        foreach (var dictionary in _dictionaries)
        {
            if (dictionary.Remove(index, out var value))
            {
                return value;
            }
        }

        var newDictionary = CreateNewDictionary();

        newDictionary.Remove(index, out var value2);

        return value2!;
    }

    private Dictionary<int, T> CreateNewDictionary()
    {
        var list = _enumerable.ToList();
        
        _count = list.Count;

        var newDictionary = list.ToDictionary(x => list.IndexOf(x), x => x);
        
        _dictionaries.Add(newDictionary);

        return newDictionary;
    }
}