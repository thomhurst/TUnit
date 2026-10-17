namespace TUnit.Core;

/// <summary>
/// Represents the context for test discovery.
/// </summary>
public class TestDiscoveryContext : Context
{
    public static new TestDiscoveryContext? Current
    {
        get => AmbientContexts.Current?.Discovery;
        internal set => AmbientContexts.SetDiscovery(value);
    }

    internal TestDiscoveryContext(BeforeTestDiscoveryContext parent) : base(parent)
    {
    }

    internal MutableTestSet TestSet { get; } = new("test discovery");

    /// <summary>
    /// Replaces the discovered tests with a subset of them. Tests left out are excluded, as
    /// with <see cref="ExcludeTests"/>; a test passed more than once counts once. Only valid until the
    /// <c>[After(TestDiscovery)]</c> hooks have finished.
    /// </summary>
    /// <exception cref="ArgumentException">A test was not discovered in this run.</exception>
    /// <exception cref="InvalidOperationException">The <c>[After(TestDiscovery)]</c> hooks have already finished.</exception>
    public void AddTests(IEnumerable<TestContext> tests)
    {
        TestSet.SetTests(tests);
    }

    public BeforeTestDiscoveryContext BeforeTestDiscoveryContext => (BeforeTestDiscoveryContext) Parent!;

    public required string? TestFilter { get; init; }

    /// <summary>
    /// Gets the assemblies of the tests in <see cref="AllTests"/>, in first-occurrence order.
    /// </summary>
    public IEnumerable<AssemblyHookContext> Assemblies => TestSet.Assemblies;

    /// <summary>
    /// Gets the classes of the tests in <see cref="AllTests"/>, in first-occurrence order.
    /// </summary>
    public IEnumerable<ClassHookContext> TestClasses => TestSet.TestClasses;

    /// <summary>
    /// Gets every discovered test, before framework filtering (tree-node filters, <c>[Explicit]</c>) is applied.
    /// </summary>
    public IReadOnlyList<TestContext> AllTests => TestSet.Tests;

    /// <summary>
    /// Removes the matching tests from the run. Excluded tests are not reported and are not listed by discovery.
    /// A test that a remaining test depends on (<c>[DependsOn]</c>) still runs.
    /// </summary>
    /// <param name="predicate">Returns <see langword="true"/> for tests to exclude.</param>
    /// <exception cref="InvalidOperationException">The <c>[After(TestDiscovery)]</c> hooks have already finished.</exception>
    public void ExcludeTests(Func<TestContext, bool> predicate) => TestSet.Exclude(predicate);

    /// <summary>
    /// Marks the matching tests as skipped. They stay in the run and are reported as skipped with <paramref name="reason"/>.
    /// Tests that are already skipped keep their original reason, and the first reason wins across calls.
    /// Skips take effect when the stage ends, and only for tests still in the set; if the predicate throws,
    /// nothing is skipped. The test filter (such as <c>--treenode-filter</c>) runs afterwards, so a skipped test
    /// it removes is not reported at all.
    /// </summary>
    /// <param name="predicate">Returns <see langword="true"/> for tests to skip.</param>
    /// <param name="reason">The skip reason reported for each matching test.</param>
    /// <exception cref="InvalidOperationException">The <c>[After(TestDiscovery)]</c> hooks have already finished.</exception>
    public void SkipTests(Func<TestContext, bool> predicate, string reason) => TestSet.Skip(predicate, reason);

    /// <summary>
    /// Orders the tests by a key. The order decides when tests start. Tests with equal keys keep the default
    /// order among themselves (class, then <c>[ExecutionPriority]</c>, then <c>[NotInParallel(Order)]</c>), so use
    /// a key that distinguishes every test for a total order. <c>[ExecutionPriority]</c> still takes precedence for
    /// tests that can run in parallel, including keyed <c>[NotInParallel("key")]</c> tests, and tests with
    /// different keys can still overlap. The order applies within each scheduling phase (parallel,
    /// parallel groups, global <c>[NotInParallel]</c>) and never moves a test to another phase. Tests without a
    /// rank, such as <c>[DependsOn]</c> targets re-included after being excluded, start before ranked tests in
    /// the same phase. Calls don't compose: the last call wins, so combine
    /// keys into one, such as <c>(category, name)</c>. The order takes effect when the stage ends.
    /// </summary>
    /// <param name="keySelector">Selects the sort key for a test.</param>
    /// <param name="comparer">Compares keys. Defaults to <see cref="Comparer{T}.Default"/>.</param>
    /// <exception cref="InvalidOperationException">The <c>[After(TestDiscovery)]</c> hooks have already finished.</exception>
    public void OrderTestsBy<TKey>(Func<TestContext, TKey> keySelector, IComparer<TKey>? comparer = null) => TestSet.OrderBy(keySelector, comparer);

    internal override void SetAsyncLocalContext()
    {
        Current = this;
    }
}
