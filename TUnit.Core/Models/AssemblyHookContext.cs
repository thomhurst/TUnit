using System.Diagnostics;
using System.Reflection;

namespace TUnit.Core;

[DebuggerDisplay("{Assembly.GetName().Name}")]
public class AssemblyHookContext : Context
{
    private static readonly AsyncLocal<AssemblyHookContext?> Contexts = new();
    public static new AssemblyHookContext? Current
    {
        get => Contexts.Value;
        internal set
        {
            Contexts.Value = value;
            TestSessionContext.Current = value?.TestSessionContext;
        }
    }

    internal AssemblyHookContext(TestSessionContext testSessionContext) : base(testSessionContext)
    {
        testSessionContext.AddAssembly(this);
    }

    public TestSessionContext TestSessionContext => (TestSessionContext) Parent!;

    public required Assembly Assembly { get; init; }

    private readonly List<ClassHookContext> _testClasses = [];
    private TestContext[]? _cachedAllTests;

    public void AddClass(ClassHookContext classHookContext)
    {
        _testClasses.Add(classHookContext);
        InvalidateCache();
    }

    public IReadOnlyList<ClassHookContext> TestClasses => _testClasses;

    public IReadOnlyList<TestContext> AllTests => _cachedAllTests ??= TestClasses.SelectMany(x => x.Tests).ToArray();

    public int TestCount => AllTests.Count;

    private void InvalidateCache()
    {
        _cachedAllTests = null;
    }

    internal bool FirstTestStarted { get; set; }

    internal void RemoveClass(ClassHookContext classContext)
    {
        _testClasses.Remove(classContext);
        InvalidateCache();

        if (_testClasses.Count == 0)
        {
            TestSessionContext.RemoveAssembly(this);
        }
    }

    internal override void SetAsyncLocalContext()
    {
        Current = this;
    }
}
