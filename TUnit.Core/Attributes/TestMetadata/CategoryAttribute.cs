namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly)]
public class CategoryAttribute : TUnitAttribute
{
    public string Category { get; }

    public CategoryAttribute(string category)
    {
        Category = category;
    }
}