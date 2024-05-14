namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Class)]
public class InjectAttribute<T> : TUnitAttribute where T : new()
{
    public SharedType Shared { get; set; } = SharedType.None;
    public string? Key { get; set; }
}

public enum SharedType
{
    None,
    ForClass,
    Globally,
    Keyed,
}