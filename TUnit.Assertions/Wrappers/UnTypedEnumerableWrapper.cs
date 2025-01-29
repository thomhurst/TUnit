using System.Collections;

namespace TUnit.Assertions.Wrappers;

internal class UnTypedEnumerableWrapper(IEnumerable enumerable) 
    : IEnumerable<object>, 
        IEquatable<UnTypedEnumerableWrapper>, 
        IEquatable<IEnumerable>
{
    public IEnumerable Enumerable { get; } = enumerable;

    public IEnumerator<object> GetEnumerator()
    {
        return Enumerable.Cast<object>().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return Enumerable.GetEnumerator();
    }

    public bool Equals(UnTypedEnumerableWrapper? other)
    {
        return Equals(other?.Enumerable, Enumerable);
    }

    public bool Equals(IEnumerable? other)
    {
        return other?.Equals(Enumerable) is true;
    }
}