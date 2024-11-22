using System.Diagnostics.CodeAnalysis;

namespace TUnit.Assertions.Equality;

public class EquivalentToEqualityComparer<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(CompareOptions compareOptions) : IEqualityComparer<T>
{
    public EquivalentToEqualityComparer() : this(new CompareOptions())
    {
    }
    
    public bool Equals(T? x, T? y)
    {
        return !Compare.CheckEquivalent(x, y, compareOptions).Any();
    }

    public int GetHashCode([DisallowNull] T obj)
    {
        return obj.GetHashCode();
    }
}