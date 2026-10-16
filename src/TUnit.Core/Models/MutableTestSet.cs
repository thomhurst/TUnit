namespace TUnit.Core;

/// <summary>
/// The test set behind <see cref="TestDiscoveryContext"/> and <see cref="TestFilteringContext"/>:
/// hooks exclude, skip and order tests through it until the engine seals it.
/// </summary>
internal sealed class MutableTestSet(string stageName)
{
    private IReadOnlyList<TestContext> _tests = [];
    private ClassHookContext[]? _testClasses;
    private AssemblyHookContext[]? _assemblies;

    public IReadOnlyList<TestContext> Tests => _tests;

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

    public void Seal() => IsSealed = true;

    public void SetTests(IEnumerable<TestContext> tests)
    {
        ThrowIfSealed();
        Replace(tests as IReadOnlyList<TestContext> ?? tests.ToArray());
        IsModified = true;
    }

    public void Exclude(Func<TestContext, bool> predicate)
    {
        if (predicate is null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }

        ThrowIfSealed();

        var tests = _tests;
        List<TestContext>? kept = null;
        for (var i = 0; i < tests.Count; i++)
        {
            var test = tests[i];
            if (predicate(test))
            {
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

        foreach (var test in _tests)
        {
            // Keep the first reason: a test skipped by [Skip] or an earlier hook stays as it was.
            if (string.IsNullOrEmpty(test.SkipReason) && predicate(test))
            {
                test.SkipReason = reason;
                test.Metadata.TestDetails.ClassInstance = SkippedTestInstance.Instance;
            }
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
        // (class, [NotInParallel(Order)]) among them. Ranks start at 1; 0 means "not ordered".
        var ordered = new TestContext[tests.Count];
        var rank = 0;
        for (var i = 0; i < indices.Length; i++)
        {
            if (i == 0 || comparer.Compare(keys[indices[i - 1]], keys[indices[i]]) != 0)
            {
                rank++;
            }

            var test = tests[indices[i]];
            test.HookRank = rank;
            ordered[i] = test;
        }

        Replace(ordered);
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
