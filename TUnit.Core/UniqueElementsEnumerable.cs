using System.Collections;

namespace TUnit.Core;

public class UniqueElementsEnumerable<T> : IEnumerable<UniqueElementsEnumerable<T>.UniqueElementAccessor>
{
    private readonly IEnumerable<Func<T>> _enumerable;

    public UniqueElementsEnumerable(IEnumerable<T> enumerable)
    {
        _enumerable = enumerable.Select<T, Func<T>>(x => () => x);
    }
    
    public IEnumerator<UniqueElementAccessor> GetEnumerator()
    {
        return _enumerable.Select(func => new UniqueElementAccessor(func)).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
    
    public readonly struct UniqueElementAccessor
    {
        private readonly Func<T> _func;

        internal UniqueElementAccessor(Func<T> func)
        {
            _func = func;
        }

        public T Get() => _func();
    }
}