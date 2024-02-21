namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class NotInParallelAttribute : Attribute
{
    public string[] ConstraintKeys { get; } = Array.Empty<string>();

    public int Order { get; init; }
    
    public NotInParallelAttribute()
    {
    }
    
    public NotInParallelAttribute(string constraintKey) : this([constraintKey])
    {
        ArgumentException.ThrowIfNullOrEmpty(constraintKey);
    }
    
    public NotInParallelAttribute(string[] constraintKeys)
    {
        ConstraintKeys = constraintKeys;
    }
}