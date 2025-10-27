using System.Collections.Concurrent;

namespace TUnit.Core.Services;

/// <summary>
/// Service to limit the number of shrink attempts per property test to prevent infinite loops
/// </summary>
public class ShrinkLimiter
{
    private readonly ConcurrentDictionary<Guid, int> _attemptCounts = new();
    private readonly int _maxAttempts;

    /// <summary>
    /// Creates a new shrink limiter with the specified maximum attempts
    /// </summary>
    /// <param name="maxAttempts">Maximum number of shrink attempts allowed per test</param>
    public ShrinkLimiter(int maxAttempts = 1000)
    {
        _maxAttempts = maxAttempts;
    }

    /// <summary>
    /// Maximum number of shrink attempts allowed
    /// </summary>
    public int MaxAttempts => _maxAttempts;

    /// <summary>
    /// Check if more shrinking is allowed for the given test
    /// </summary>
    /// <param name="originalTestId">The ID of the original failing test</param>
    /// <returns>True if shrinking can continue, false if limit reached</returns>
    public bool CanShrink(Guid originalTestId)
    {
        var count = _attemptCounts.GetOrAdd(originalTestId, 0);
        return count < _maxAttempts;
    }

    /// <summary>
    /// Increment the shrink attempt counter and return the new count
    /// </summary>
    /// <param name="originalTestId">The ID of the original failing test</param>
    /// <returns>The new attempt count after incrementing</returns>
    public int IncrementAndGet(Guid originalTestId)
    {
        return _attemptCounts.AddOrUpdate(originalTestId, 1, (_, count) => count + 1);
    }

    /// <summary>
    /// Get the current attempt count for a test
    /// </summary>
    /// <param name="originalTestId">The ID of the original failing test</param>
    /// <returns>The current attempt count</returns>
    public int GetAttemptCount(Guid originalTestId)
    {
        return _attemptCounts.GetOrAdd(originalTestId, 0);
    }

    /// <summary>
    /// Reset the attempt count for a specific test
    /// </summary>
    /// <param name="originalTestId">The ID of the original failing test</param>
    public void Reset(Guid originalTestId)
    {
        _attemptCounts.TryRemove(originalTestId, out _);
    }

    /// <summary>
    /// Clear all attempt counts
    /// </summary>
    public void ResetAll()
    {
        _attemptCounts.Clear();
    }
}
