namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
public class PropertyAttribute(string name, string value) : TUnitAttribute
{
    public string Name { get; } = name;
    public string Value { get; } = value;
}