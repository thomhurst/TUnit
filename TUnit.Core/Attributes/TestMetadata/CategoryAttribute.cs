namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class CategoryAttribute : TUnitAttribute
{
    public string Category { get; }

    public CategoryAttribute(string category)
    {
        Category = category;
    }
}