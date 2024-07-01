namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly)]
public class NotInParallelAttribute : SingleTUnitAttribute
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
        if (constraintKeys.Length != constraintKeys.Distinct().Count())
        {
            throw new ArgumentException("Duplicate constraint keys are not allowed.");
        }
        
        ConstraintKeys = constraintKeys;
    }
}