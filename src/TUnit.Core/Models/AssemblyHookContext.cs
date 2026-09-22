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

    public void AddClass(ClassHookContext classHookContext)
    {
        lock (_lock)
        {
            _testClasses.Add(classHookContext);
            InvalidateCache();
        }
    }

    public IReadOnlyList<ClassHookContext> TestClasses { get { lock (_lock) return [.. _testClasses]; } }

    public IReadOnlyList<TestContext> AllTests => _cachedAllTests ??= TestClasses.SelectMany(x => x.Tests).ToArray();

    public int TestCount => AllTests.Count;

    private void InvalidateCache()
    {
        _cachedAllTests = null;
    }

    internal bool FirstTestStarted { get; set; }

    internal void RemoveClass(ClassHookContext classContext)
    {
        bool empty;
        lock (_lock)
        {
            _testClasses.Remove(classContext);
            InvalidateCache();
            empty = _testClasses.Count == 0;
        }

        if (empty)
        {
            TestSessionContext.RemoveAssembly(this);
        }
    }

    internal override void SetAsyncLocalContext()
    {
        Current = this;
    }
}
