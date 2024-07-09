namespace TUnit.Core;

public class OrderableTUnitAttribute : Attribute
{
    internal OrderableTUnitAttribute()
    {
    }
    
    public int Order { get; init; }
}