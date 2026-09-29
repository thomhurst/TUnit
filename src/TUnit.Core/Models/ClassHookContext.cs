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

    // Set when the last test was removed and the class was detached from its assembly. Guarded by _lock.
    private bool _detached;

    public void AddTest(TestContext testContext)
    {
        lock (_lock)
        {
            if (!_testSet.Add(testContext))
            {
                return; // Prevent duplicates
            }
            _tests.Add(testContext);

            // A dynamic test added at run time to a class whose built tests were all filtered out.
            // Reattach under _lock so a concurrent detach cannot interleave with it.
            if (_detached)
            {
                _detached = false;
                AssemblyContext.AddClass(this);
            }
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
        lock (_lock)
        {
            if (_tests.RemoveAll(t => !testsToKeep.Contains(t)) == 0)
            {
                return;
            }

            _testSet.IntersectWith(testsToKeep);

            if (_tests.Count is 0)
            {
                Detach();
                return;
            }
        }

        AssemblyContext.InvalidateTestCaches();
    }

    internal void RemoveTest(TestContext test)
    {
        lock (_lock)
        {
            _testSet.Remove(test);
            _tests.Remove(test);

            if (_tests.Count is 0)
            {
                Detach();
            }
        }
    }

    // Caller holds _lock. Lock order is class -> assembly -> session; the parents never call back
    // into a class while holding their own lock.
    private void Detach()
    {
        if (_detached)
        {
            return;
        }

        _detached = true;
        AssemblyContext.RemoveClass(this);
    }

    internal override void SetAsyncLocalContext()
    {
        Current = this;
    }
}
