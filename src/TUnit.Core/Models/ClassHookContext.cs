using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Helpers;

namespace TUnit.Core;

[DebuggerDisplay("{ClassType.Name}")]
public class ClassHookContext : Context
{
    private static readonly AsyncLocal<ClassHookContext?> Contexts = new();
    public static new ClassHookContext? Current
    {
        get => Contexts.Value;
        internal set
        {
            Contexts.Value = value;
            AssemblyHookContext.Current = value?.AssemblyContext;
        }
    }

    internal ClassHookContext(AssemblyHookContext assemblyHookContext) : base(assemblyHookContext)
    {
        assemblyHookContext.AddClass(this);
    }

    public AssemblyHookContext AssemblyContext => (AssemblyHookContext) Parent!;

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
    public required Type ClassType { get; init; }

    private readonly Lock _lock = new();
    private readonly HashSet<TestContext> _testSet = new(ReferenceEqualityComparer<TestContext>.Instance);
    private readonly List<TestContext> _tests = [];

    public void AddTest(TestContext testContext)
    {
        lock (_lock)
        {
            if (!_testSet.Add(testContext))
            {
                return; // Prevent duplicates
            }
            _tests.Add(testContext);
        }
    }

    public IReadOnlyList<TestContext> Tests { get { lock (_lock) return [.. _tests]; } }

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
        bool empty;
        lock (_lock)
        {
            _testSet.Remove(test);
            _tests.Remove(test);
            empty = _tests.Count is 0;
        }

        if (empty)
        {
            AssemblyContext.RemoveClass(this);
        }
    }

    internal override void SetAsyncLocalContext()
    {
        Current = this;
    }
}
