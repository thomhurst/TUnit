using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

[DebuggerDisplay("{ClassType.Name}")]
public class ClassHookContext : Context
{
    private static readonly AsyncLocal<ClassHookContext?> Contexts = new();
    public static new ClassHookContext? Current
    {
        get => Contexts.Value;
        internal set => Contexts.Value = value;
    }

    internal ClassHookContext(AssemblyHookContext assemblyHookContext) : base(assemblyHookContext)
    {
        assemblyHookContext.AddClass(this);
    }

    public AssemblyHookContext AssemblyContext => (AssemblyHookContext) Parent!;

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
    public required Type ClassType { get; init; }

    private readonly List<TestContext> _tests = [];

    public void AddTest(TestContext testContext)
    {
        _tests.Add(testContext);
    }

    public IReadOnlyList<TestContext> Tests => _tests;

    public int TestCount => Tests.Count;
    internal bool FirstTestStarted { get; set; }

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

        return Equals((ClassHookContext) obj);
    }

    public override int GetHashCode()
    {
        return ClassType.GetHashCode();
    }

    internal void RemoveTest(TestContext test)
    {
        _tests.Remove(test);

        if (_tests.Count is 0)
        {
            AssemblyContext.RemoveClass(this);
        }
    }

    internal override void RestoreContextAsyncLocal()
    {
        Current = this;
    }
}
