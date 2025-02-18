using System.Diagnostics.CodeAnalysis;

namespace TUnit.Assertions.Equality;

#pragma warning disable CS8767
public class CollectionEquivalentToEqualityComparer<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] T>(
    CompareOptions compareOptions) : EquivalentToEqualityComparer<T>(compareOptions)
{
    public CollectionEquivalentToEqualityComparer() : this(new CompareOptions())
    {
    }
    
    public override int? EnumerableIndex { get; protected set; } = 0;
    
    public override bool Equals(T? x, T? y)
    {
        try
        {
            return base.Equals(x, y);
        }
        finally
        {
            EnumerableIndex++;
        }
    }
}