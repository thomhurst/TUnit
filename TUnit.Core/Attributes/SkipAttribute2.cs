namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class PropertyAttribute : TUnitAttribute
{
    public string Name { get; }
    public string Value { get; }

    public PropertyAttribute(string name, string value)
    {
        Name = name;
        Value = value;
    }
}