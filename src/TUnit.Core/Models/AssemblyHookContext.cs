using System.Diagnostics;
using System.Reflection;

namespace TUnit.Core;

[DebuggerDisplay("{Assembly.GetName().Name}")]
public class AssemblyHookContext : Context
{
    public static new AssemblyHookContext? Current
    {
        get => AmbientContexts.Current?.Assembly;
        // Cascades to the session/discovery contexts in a single AsyncLocal write.
        internal set => AmbientContexts.SetAssembly(value);
    }

    internal AssemblyHookContext(TestSessionContext testSessionContext) : base(testSessionContext)
    {
        testSessionContext.AddAssembly(this);
    }

    public TestSessionContext TestSessionContext => (TestSessionContext) Parent!;

    public required Assembly Assembly { get; init; }

    private readonly Lock _lock = new();
    internal Lock SynchronizationLock => _lock;
    private readonly List<ClassHookContext> _testClasses = [];
    private TestContext[]? _cachedAllTests;

    // Set when the last class was removed and the assembly was detached from the session. Guarded by _lock.
    private bool _detached;

    public void AddClass(ClassHookContext classHookContext)
    {
        lock (_lock)
        {
            _testClasses.Add(classHookContext);
            InvalidateCache();

            // A class reattached by a dynamic test after filtering emptied this assembly.
            if (_detached)
            {
                _detached = false;
                TestSessionContext.AddAssembly(this);
                return;
            }
        }

        TestSessionContext.InvalidateTestCaches();
    }

    public IReadOnlyList<ClassHookContext> TestClasses { get { lock (_lock) return [.. _testClasses]; } }

    public IReadOnlyList<TestContext> AllTests => _cachedAllTests ??= TestClasses.SelectMany(x => x.Tests).ToArray();

    public int TestCount => AllTests.Count;

    private void InvalidateCache()
    {
        _cachedAllTests = null;
    }

    internal bool FirstTestStarted { get; set; }

    internal void InvalidateTestCaches()
    {
        lock (_lock)
        {
            InvalidateCache();
        }

        TestSessionContext.InvalidateTestCaches();
    }

    internal void RemoveClass(ClassHookContext classContext)
    {
        lock (_lock)
        {
            _testClasses.Remove(classContext);
            InvalidateCache();

            if (_testClasses.Count == 0)
            {
                _detached = true;
                TestSessionContext.RemoveAssembly(this);
                return;
            }
        }

        TestSessionContext.InvalidateTestCaches();
    }

    internal override void SetAsyncLocalContext()
    {
        Current = this;
    }
}
