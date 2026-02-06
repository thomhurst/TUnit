namespace TUnit.Engine.Scheduling;

/// <summary>
/// Represents a test that is waiting for its constraint keys to become available.
/// </summary>
internal sealed class WaitingTest
{
    public required string TestId { get; init; }
    public required IReadOnlyList<string> ConstraintKeys { get; init; }
    public required TaskCompletionSource<bool> StartSignal { get; init; }
    public required int Priority { get; init; }
}

/// <summary>
/// Index structure that maps constraint keys to waiting tests, enabling O(k) lookup
/// when keys are released instead of scanning the entire queue.
/// All operations must be performed under the caller's lock.
/// </summary>
internal sealed class WaitingTestIndex
{
    // Maps each constraint key to the set of tests waiting on that key
    private readonly Dictionary<string, HashSet<WaitingTest>> _keyToTests = new();

    // Master set of all waiting tests (for fast membership checks and count)
    private readonly HashSet<WaitingTest> _allTests = new();

    /// <summary>
    /// Gets the number of waiting tests currently in the index.
    /// </summary>
    public int Count => _allTests.Count;

    /// <summary>
    /// Adds a waiting test to all key indexes.
    /// </summary>
    public void Add(WaitingTest waitingTest)
    {
        _allTests.Add(waitingTest);

        var keys = waitingTest.ConstraintKeys;
        var keyCount = keys.Count;
        for (var i = 0; i < keyCount; i++)
        {
            var key = keys[i];
            if (!_keyToTests.TryGetValue(key, out var tests))
            {
                tests = new HashSet<WaitingTest>();
                _keyToTests[key] = tests;
            }
            tests.Add(waitingTest);
        }
    }

    /// <summary>
    /// Removes a waiting test from all key indexes.
    /// </summary>
    public void Remove(WaitingTest waitingTest)
    {
        if (!_allTests.Remove(waitingTest))
        {
            return;
        }

        var keys = waitingTest.ConstraintKeys;
        var keyCount = keys.Count;
        for (var i = 0; i < keyCount; i++)
        {
            var key = keys[i];
            if (_keyToTests.TryGetValue(key, out var tests))
            {
                tests.Remove(waitingTest);
                if (tests.Count == 0)
                {
                    _keyToTests.Remove(key);
                }
            }
        }
    }

    /// <summary>
    /// Returns a deduplicated set of waiting tests that are associated with any of the released keys.
    /// These are candidates that might be unblocked (but still need to be checked against locked keys).
    /// </summary>
    public HashSet<WaitingTest> GetCandidatesForReleasedKeys(IReadOnlyList<string> releasedKeys)
    {
        var candidates = new HashSet<WaitingTest>();

        var keyCount = releasedKeys.Count;
        for (var i = 0; i < keyCount; i++)
        {
            if (_keyToTests.TryGetValue(releasedKeys[i], out var tests))
            {
                // HashSet.UnionWith handles deduplication
                candidates.UnionWith(tests);
            }
        }

        return candidates;
    }
}
