namespace TUnit.Core;

/// <summary>
/// Represents the context for test filtering: the tests that passed framework filtering
/// (tree-node filters, <c>[Explicit]</c>) and will run in this session.
/// </summary>
/// <remarks>
/// <c>[Before(TestFiltering)]</c> hooks can still exclude, skip and order tests.
/// <c>[After(TestFiltering)]</c> hooks see the final set and cannot change it.
/// These hooks run only when tests are executed, not for discovery-only requests such as <c>--list-tests</c>.
/// </remarks>
public class TestFilteringContext : Context
{
    private static readonly AsyncLocal<TestFilteringContext?> Contexts = new();

    /// <summary>
    /// Gets the current test filtering context.
    /// </summary>
    public static new TestFilteringContext? Current
    {
        get => Contexts.Value;
        internal set => Contexts.Value = value;
    }

    internal TestFilteringContext(TestDiscoveryContext parent) : base(parent)
    {
    }

    internal MutableTestSet TestSet { get; } = new("test filtering");

    public TestDiscoveryContext TestDiscoveryContext => (TestDiscoveryContext) Parent!;

    /// <summary>
    /// Gets the test filter of this run, if any.
    /// </summary>
    public required string? TestFilter { get; init; }

    public IEnumerable<AssemblyHookContext> Assemblies => TestSet.Assemblies;

    public IEnumerable<ClassHookContext> TestClasses => TestSet.TestClasses;

    /// <summary>
    /// Gets the tests that will run, including tests that are re-included because a remaining test depends on them.
    /// </summary>
    public IReadOnlyList<TestContext> AllTests => TestSet.Tests;

    /// <summary>
    /// Removes the matching tests from the run. Excluded tests are not reported.
    /// A test that a remaining test depends on (<c>[DependsOn]</c>) still runs.
    /// </summary>
    /// <param name="predicate">Returns <see langword="true"/> for tests to exclude.</param>
    /// <exception cref="InvalidOperationException">Called from an <c>[After(TestFiltering)]</c> hook.</exception>
    public void ExcludeTests(Func<TestContext, bool> predicate) => TestSet.Exclude(predicate);

    /// <summary>
    /// Marks the matching tests as skipped. They stay in the run and are reported as skipped with <paramref name="reason"/>.
    /// Tests that are already skipped keep their original reason.
    /// </summary>
    /// <param name="predicate">Returns <see langword="true"/> for tests to skip.</param>
    /// <param name="reason">The skip reason reported for each matching test.</param>
    /// <exception cref="InvalidOperationException">Called from an <c>[After(TestFiltering)]</c> hook.</exception>
    public void SkipTests(Func<TestContext, bool> predicate, string reason) => TestSet.Skip(predicate, reason);

    /// <summary>
    /// Orders the tests by a key. The order decides when tests start. Tests with equal keys keep the default
    /// order among themselves (class, then <c>[NotInParallel(Order)]</c>), so use a key that distinguishes
    /// every test for a total order. <c>[ExecutionPriority]</c> still takes precedence for tests that can run
    /// in parallel, and those tests can still overlap.
    /// </summary>
    /// <param name="keySelector">Selects the sort key for a test.</param>
    /// <param name="comparer">Compares keys. Defaults to <see cref="Comparer{T}.Default"/>.</param>
    /// <exception cref="InvalidOperationException">Called from an <c>[After(TestFiltering)]</c> hook.</exception>
    public void OrderTestsBy<TKey>(Func<TestContext, TKey> keySelector, IComparer<TKey>? comparer = null) => TestSet.OrderBy(keySelector, comparer);

    internal override void SetAsyncLocalContext()
    {
        Current = this;
    }
}
