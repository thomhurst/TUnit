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
    public IEnumerable<AssemblyHookContext> Assemblies => field ??= TestClasses.Select(x => x.AssemblyContext).Distinct().ToArray();

    [field: AllowNull, MaybeNull]
    public IEnumerable<ClassHookContext> TestClasses => field ??= AllTests.Where(x => x.ClassContext != null).Select(x => x.ClassContext!).Distinct().ToArray();

    public IReadOnlyList<TestContext> AllTests { get; private set; } = [];

    internal override void RestoreContextAsyncLocal()
    {
        Current = this;
    }
}
