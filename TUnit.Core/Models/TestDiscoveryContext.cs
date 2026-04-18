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
        // HashSet<T> iteration order is not contractually guaranteed; the parallel list
        // preserves first-occurrence order so hook execution sequence stays deterministic.
        var seen = new HashSet<AssemblyHookContext>();
        var ordered = new List<AssemblyHookContext>();
        foreach (var cls in TestClasses)
        {
            if (seen.Add(cls.AssemblyContext))
            {
                ordered.Add(cls.AssemblyContext);
            }
        }
        return ordered.ToArray();
    }

    private ClassHookContext[] BuildUniqueTestClasses()
    {
        var seen = new HashSet<ClassHookContext>();
        var ordered = new List<ClassHookContext>();
        foreach (var test in AllTests)
        {
            var cls = test.ClassContext;
            if (cls != null && seen.Add(cls))
            {
                ordered.Add(cls);
            }
        }
        return ordered.ToArray();
    }

    public IReadOnlyList<TestContext> AllTests { get; private set; } = [];

    internal override void SetAsyncLocalContext()
    {
        Current = this;
    }
}
