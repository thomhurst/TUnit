using TUnit.Core;

namespace TUnit.Engine.Interfaces;

/// <summary>
/// Thread-safe queue for managing dynamically created tests during execution.
/// Ensures tests created at runtime (via CreateTestVariant or AddDynamicTest) are properly scheduled.
/// Handles discovery notification internally to keep all dynamic test logic in one place.
/// </summary>
internal interface IDynamicTestQueue
{
    /// <summary>
    /// Enqueues a test for execution and notifies the message bus. Thread-safe.
    /// </summary>
    /// <param name="test">The test to enqueue</param>
    /// <returns>Task that completes when the test is enqueued and discovery is notified</returns>
    Task EnqueueAsync(AbstractExecutableTest test);

    /// <summary>
    /// Attempts to dequeue the next test. Thread-safe.
    /// </summary>
    /// <param name="test">The dequeued test, or null if queue is empty</param>
    /// <returns>True if a test was dequeued, false if queue is empty</returns>
    bool TryDequeue(out AbstractExecutableTest? test);

    /// <summary>
    /// Gets the number of pending tests in the queue.
    /// </summary>
    int PendingCount { get; }

    /// <summary>
    /// Indicates whether the queue has been completed and no more tests will be added.
    /// </summary>
    bool IsCompleted { get; }

    /// <summary>
    /// Marks the queue as complete, indicating no more tests will be added.
    /// </summary>
    void Complete();
}
