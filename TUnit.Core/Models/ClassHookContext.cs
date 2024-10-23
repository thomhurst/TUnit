namespace TUnit.Core;

public class ClassHookContext : Context
{
    private static readonly AsyncLocal<ClassHookContext?> Contexts = new();
    public new static ClassHookContext? Current
    {
        get => Contexts.Value;
        internal set => Contexts.Value = value;
    }
    
    internal ClassHookContext()
    {
    }
    
    public required Type ClassType { get; init; }
    
    public List<TestContext> Tests { get; init; } = [];

    public int TestCount => Tests.Count;

    private bool Equals(ClassHookContext other)
    {
        return ClassType == other.ClassType;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != GetType())
        {
            return false;
        }

        return Equals((ClassHookContext)obj);
    }

    public override int GetHashCode()
    {
        return ClassType.GetHashCode();
    }
}