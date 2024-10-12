namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly)]
public class CategoryAttribute(string category) : TUnitAttribute
{
    public string Category { get; } = category;
}