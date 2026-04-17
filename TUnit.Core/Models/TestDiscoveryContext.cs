using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

/// <summary>
/// Represents the context for test discovery.
/// </summary>
public class TestDiscoveryContext : Context
{
    private static readonly AsyncLocal<TestDiscoveryContext?> Contexts = new();
    public static new TestDiscoveryContext? Current
    {
        get => Contexts.Value;
        internal set => Contexts.Value = value;
    }

    internal TestDiscoveryContext(BeforeTestDiscoveryContext parent) : base(parent)
    {
    }

    public void AddTests(IEnumerable<TestContext> tests)
    {
        AllTests = tests as IReadOnlyList<TestContext> ?? tests.ToArray();
    }

    public BeforeTestDiscoveryContext BeforeTestDiscoveryContext => (BeforeTestDiscoveryContext) Parent!;

    public required string? TestFilter { get; init; }

    [field: AllowNull, MaybeNull]
    public IEnumerable<AssemblyHookContext> Assemblies => field ??= BuildUniqueAssemblies();

    [field: AllowNull, MaybeNull]
    public IEnumerable<ClassHookContext> TestClasses => field ??= BuildUniqueTestClasses();

    private AssemblyHookContext[] BuildUniqueAssemblies()
    {
        var seen = new HashSet<AssemblyHookContext>();
        foreach (var cls in TestClasses)
        {
            seen.Add(cls.AssemblyContext);
        }
        var result = new AssemblyHookContext[seen.Count];
        seen.CopyTo(result);
        return result;
    }

    private ClassHookContext[] BuildUniqueTestClasses()
    {
        var seen = new HashSet<ClassHookContext>();
        foreach (var test in AllTests)
        {
            var cls = test.ClassContext;
            if (cls != null)
            {
                seen.Add(cls);
            }
        }
        var result = new ClassHookContext[seen.Count];
        seen.CopyTo(result);
        return result;
    }

    public IReadOnlyList<TestContext> AllTests { get; private set; } = [];

    internal override void SetAsyncLocalContext()
    {
        Current = this;
    }
}
