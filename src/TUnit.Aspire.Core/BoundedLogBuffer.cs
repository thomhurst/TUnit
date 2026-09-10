namespace TUnit.Aspire;

/// <summary>
/// A fixed-capacity, thread-safe rolling buffer of a single resource's most recent log lines.
/// Backs the retained per-resource log buffer (see <see cref="AspireFixtureOptions.RetainResourceLogs"/>)
/// so post-mortem retrieval works even after a resource has exited and its logs were dropped from
/// Aspire's <c>ResourceLoggerService</c>.
/// </summary>
/// <remarks>
/// A background pump writes lines from one thread while a diagnostics call reads from another, so
/// every operation takes the same lock. Memory is bounded to <see cref="Capacity"/> lines: once full,
/// the oldest line is dropped as each new one arrives (last-N wins, matching the diagnostic need).
/// </remarks>
internal sealed class BoundedLogBuffer(int capacity)
{
    private readonly Queue<string> _lines = new();
    private readonly object _gate = new();

    /// <summary>The maximum number of lines retained. At least 1 (a zero/negative request is clamped).</summary>
    public int Capacity { get; } = Math.Max(1, capacity);

    /// <summary>Appends a line, evicting the oldest when the buffer is at capacity.</summary>
    public void Add(string line)
    {
        lock (_gate)
        {
            _lines.Enqueue(line);
            while (_lines.Count > Capacity)
            {
                _lines.Dequeue();
            }
        }
    }

    /// <summary>
    /// Returns a point-in-time copy of the most recent <paramref name="maxLines"/> lines (all of them
    /// when <paramref name="maxLines"/> is negative or exceeds the count), oldest first.
    /// </summary>
    public IReadOnlyList<string> Snapshot(int maxLines)
    {
        lock (_gate)
        {
            if (maxLines < 0 || maxLines >= _lines.Count)
            {
                return _lines.ToArray();
            }

            if (maxLines == 0)
            {
                return [];
            }

            // Skip the oldest lines so only the most recent `maxLines` remain.
            var skip = _lines.Count - maxLines;
            var result = new string[maxLines];
            var i = 0;
            foreach (var line in _lines)
            {
                if (i >= skip)
                {
                    result[i - skip] = line;
                }

                i++;
            }

            return result;
        }
    }
}
