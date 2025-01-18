#if !NET
#pragma warning disable CS8767 // Nullability of type parameters in type of collection doesn't match implicitly implemented member (possibly because of nullability attributes).
#endif

using System.Diagnostics.CodeAnalysis;

namespace TUnit.Assertions.Equality;

public class EquivalentToEqualityComparer<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(CompareOptions compareOptions) : IEqualityComparer<T>
{
    public ComparisonFailure[]? ComparisonFailures { get; private set; }

    public EquivalentToEqualityComparer() : this(new CompareOptions())
    {
    }
    
    public bool Equals(T? x, T? y)
    {
        ComparisonFailures = Compare.CheckEquivalent(x, y, compareOptions).ToArray();
        
        return ComparisonFailures.Length == 0;
    }

    public int GetHashCode([DisallowNull] T obj)
    {
        return obj.GetHashCode();
    }
}