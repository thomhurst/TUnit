namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class NotInParallelAttribute : Attribute
{
    public string ConstraintKey { get; }

    public NotInParallelAttribute(string constraintKey = "")
    {
        ConstraintKey = constraintKey;
    }
}