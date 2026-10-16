namespace TUnit.Core;

/// <summary>
/// The test set behind <see cref="TestDiscoveryContext"/> and <see cref="TestFilteringContext"/>:
/// hooks exclude, skip and order tests through it until the engine seals it.
/// </summary>
/// <remarks>
/// Every operation evaluates all user callbacks before it changes any state, so a callback that throws
/// leaves the set as it was. Skips and ranks are kept as pending changes and written to the
/// <see cref="TestContext"/>s only by <see cref="Seal"/>, and only for tests still in the set.
/// </remarks>
internal sealed class MutableTestSet(string stageName)
{
    private IReadOnlyList<TestContext> _tests = [];
    private Dictionary<TestContext, string>? _pendingSkips;
    private Dictionary<TestContext, int>? _pendingRanks;
    private List<TestContext>? _removed;
    private ClassHookContext[]? _testClasses;
    private AssemblyHookContext[]? _assemblies;

    public IReadOnlyList<TestContext> Tests => _tests;

    /// <summary>Tests that hooks removed from the set during this stage, so the engine needn't diff.</summary>
    public IReadOnlyList<TestContext> Removed => _removed ?? (IReadOnlyList<TestContext>) [];

    /// <summary>True when hooks changed which tests are in the set.</summary>
    public bool IsModified { get; private set; }

    /// <summary>True when hooks changed the order of the set.</summary>
    public bool WasReordered { get; private set; }

    public bool IsSealed { get; private set; }

    public ClassHookContext[] TestClasses => _testClasses ??= BuildUniqueTestClasses();

    public AssemblyHookContext[] Assemblies => _assemblies ??= BuildUniqueAssemblies();

    /// <summary>
    /// Engine entry point: replaces the set and resets every flag, so a context reused by a
    /// later request starts unsealed and unmodified.
    /// </summary>
    public void Initialize(IReadOnlyList<TestContext> tests)
    {
        Replace(tests);
        _pendingSkips = null;
        _pendingRanks = null;
        _removed = null;
        IsModified = false;
        WasReordered = false;
        IsSealed = false;
    }

    /// <summary>
    /// Replaces the set without touching the flags. Used after sealing to publish the final set.
    /// </summary>
    public void Replace(IReadOnlyList<TestContext> tests)
    {
        _tests = tests;
        _testClasses = null;
        _assemblies = null;
    }

    /// <summary>
    /// Ends the stage: applies pending skips and ranks to the tests still in the set, then rejects
    /// further changes. Tests that hooks excluded are never modified.
    /// </summary>
    public void Seal()
    {
        if (IsSealed)
        {
            return;
        }

        IsSealed = true;

        var pendingSkips = _pendingSkips;
        var pendingRanks = _pendingRanks;
        _pendingSkips = null;
        _pendingRanks = null;

        if (pendingSkips is null && pendingRanks is null)
        {
            return;
        }

        foreach (var test in _tests)
        {
            // A reason set before the stage (e.g. by [Skip]) wins over hook skips.
            if (pendingSkips is not null
                && pendingSkips.TryGetValue(test, out var reason)
                && string.IsNullOrEmpty(test.SkipReason))
            {
                test.SkipReason = reason;
                test.Metadata.TestDetails.ClassInstance = SkippedTestInstance.Instance;
            }

            if (pendingRanks is not null && pendingRanks.TryGetValue(test, out var rank))
            {
                test.HookRank = rank;
            }
        }
    }

    public void SetTests(IEnumerable<TestContext> tests)
    {
        ThrowIfSealed();

        var replacement = tests as IReadOnlyList<TestContext> ?? tests.ToArray();
        var kept = new HashSet<TestContext>(replacement, Helpers.ReferenceEqualityComparer<TestContext>.Instance);

        // A test put back after an earlier exclusion is no longer removed.
        _removed?.RemoveAll(kept.Contains);

        foreach (var test in _tests)
        {
            if (!kept.Contains(test))
            {
                (_removed ??= []).Add(test);
            }
        }

        Replace(replacement);
        IsModified = true;
    }

    public void Exclude(Func<TestContext, bool> predicate)
    {
        if (predicate is null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }

        ThrowIfSealed();

        // All-or-nothing: the kept list is local until every predicate has run.
        var tests = _tests;
        List<TestContext>? kept = null;
        List<TestContext>? removed = null;
        for (var i = 0; i < tests.Count; i++)
        {
            var test = tests[i];
            if (predicate(test))
            {
                (removed ??= []).Add(test);
                if (kept is null)
                {
                    kept = new List<TestContext>(tests.Count - 1);
                    for (var j = 0; j < i; j++)
                    {
                        kept.Add(tests[j]);
                    }
                }
            }
            else
            {
                kept?.Add(test);
            }
        }

        if (kept is not null)
        {
            Replace(kept);
            (_removed ??= []).AddRange(removed!);
            IsModified = true;
        }
    }

    public void Skip(Func<TestContext, bool> predicate, string reason)
    {
        if (predicate is null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }

        if (string.IsNullOrEmpty(reason))
        {
            throw new ArgumentException("A skip reason is required.", nameof(reason));
        }

        ThrowIfSealed();

        // Evaluate every predicate before recording anything, so a throwing predicate changes nothing.
        List<TestContext>? matches = null;
        foreach (var test in _tests)
        {
            if (predicate(test))
            {
                (matches ??= []).Add(test);
            }
        }

        if (matches is null)
        {
            return;
        }

        _pendingSkips ??= new Dictionary<TestContext, string>(Helpers.ReferenceEqualityComparer<TestContext>.Instance);
        foreach (var test in matches)
        {
            // First reason wins across calls; Seal() also keeps a reason set before the stage.
            _pendingSkips.TryAdd(test, reason);
        }
    }

    public void OrderBy<TKey>(Func<TestContext, TKey> keySelector, IComparer<TKey>? comparer)
    {
        if (keySelector is null)
        {
            throw new ArgumentNullException(nameof(keySelector));
        }

        ThrowIfSealed();

        comparer ??= Comparer<TKey>.Default;

        var tests = _tests;
        var keys = new TKey[tests.Count];
        var indices = new int[tests.Count];
        for (var i = 0; i < tests.Count; i++)
        {
            keys[i] = keySelector(tests[i]);
            indices[i] = i;
        }

        // Stable sort: the original index breaks ties.
        Array.Sort(indices, (x, y) =>
        {
            var compare = comparer.Compare(keys[x], keys[y]);
            return compare != 0 ? compare : x.CompareTo(y);
        });

        // Tests with equal keys share a rank, so the scheduler keeps its default order
        // (class, [NotInParallel(Order)]) among them. Each call replaces the previous ranks
        // (the last call wins); Seal() writes them to the tests.
        var ordered = new TestContext[tests.Count];
        var ranks = new Dictionary<TestContext, int>(tests.Count, Helpers.ReferenceEqualityComparer<TestContext>.Instance);
        var rank = 0;
        for (var i = 0; i < indices.Length; i++)
        {
            if (i == 0 || comparer.Compare(keys[indices[i - 1]], keys[indices[i]]) != 0)
            {
                rank++;
            }

            var test = tests[indices[i]];
            ranks[test] = rank;
            ordered[i] = test;
        }

        Replace(ordered);
        _pendingRanks = ranks;
        WasReordered = true;
    }

    private void ThrowIfSealed()
    {
        if (IsSealed)
        {
            throw new InvalidOperationException(
                $"Tests can no longer be changed: the {stageName} stage has finished. " +
                "Exclude, skip or order tests from an [After(TestDiscovery)] or [Before(TestFiltering)] hook.");
        }
    }

    private ClassHookContext[] BuildUniqueTestClasses()
    {
        // HashSet<T> iteration order is not contractually guaranteed; the parallel list
        // preserves first-occurrence order so hook execution sequence stays deterministic.
        var seen = new HashSet<ClassHookContext>();
        var ordered = new List<ClassHookContext>();
        foreach (var test in _tests)
        {
            var cls = test.ClassContext;
            if (cls != null && seen.Add(cls))
            {
                ordered.Add(cls);
            }
        }
        return ordered.ToArray();
    }

    private AssemblyHookContext[] BuildUniqueAssemblies()
    {
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
}
