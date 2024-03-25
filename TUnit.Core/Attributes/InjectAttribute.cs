namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Class)]
public class InjectAttribute<T> : Attribute where T : new()
{
    public SharedType Shared { get; set; } = SharedType.None;
}

public record struct ForKey(string Key) : SharedType;
public record struct Globally : SharedType;
public record struct ForClass : SharedType;
public record struct None : SharedType;

public interface SharedType
{
    public static SharedType None => new None();
    public static SharedType ForClass => new ForClass();
    public static SharedType Globally => new Globally();
    public static SharedType ForKey(string key) => new ForKey(key);
}