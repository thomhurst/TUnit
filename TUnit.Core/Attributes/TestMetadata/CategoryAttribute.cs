namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class CategoryAttribute : TUnitAttribute
{
    public string Category { get; }

    public CategoryAttribute(string category)
    {
        Category = category;
    }
}