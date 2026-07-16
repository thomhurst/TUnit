namespace TUnit.Engine;

/// <summary>
/// Trait property
/// </summary>
public class Trait
{
    public string Name { get; }
    public string Value { get; }

    public Trait(string name, string value)
    {
        Name = name;
        Value = value;
    }
}
