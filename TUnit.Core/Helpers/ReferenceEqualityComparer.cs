namespace TUnit.Core.Helpers;

public class ReferenceEqualityComparer : IEqualityComparer<object>
{
    public new bool Equals(object? x, object? y)
    {
        return ReferenceEquals(x, y);
    }

    public int GetHashCode(object obj)
    {
        return obj.GetHashCode();
    }
}
