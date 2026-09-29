using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Helpers;

namespace TUnit.Core;

[DebuggerDisplay("{ClassType.Name}")]
public class ClassHookContext : Context
{
    public static new ClassHookContext? Current
    {
        get => AmbientContexts.Current?.Class;
        // Cascades to the assembly/session/discovery contexts in a single AsyncLocal write.
        internal set => AmbientContexts.SetClass(value);
    }

    internal ClassHookContext(AssemblyHookContext assemblyHookContext) : base(assemblyHookContext)
    {
        assemblyHookContext.AddClass(this);
    }

    public AssemblyHookContext AssemblyContext => (AssemblyHookContext) Parent!;

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
    public required Type ClassType { get; init; }

    private readonly Lock _lock = new();
    internal Lock SynchronizationLock => _lock;
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

    /// <summary>
    /// Drops every test not in <paramref name="testsToKeep"/>. Used once filtering has decided which
    /// built tests will run, so hooks only see participating tests. Removes the class from its assembly
    /// when no tests remain.
    /// </summary>
    internal void RetainTests(HashSet<TestContext> testsToKeep)
    {
        bool empty;
        lock (_lock)
        {
            if (_tests.RemoveAll(t => !testsToKeep.Contains(t)) == 0)
            {
                return;
            }

            _testSet.IntersectWith(testsToKeep);
            empty = _tests.Count is 0;
        }

        if (empty)
        {
            AssemblyContext.RemoveClass(this);
        }
        else
        {
            AssemblyContext.InvalidateTestCaches();
        }
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
