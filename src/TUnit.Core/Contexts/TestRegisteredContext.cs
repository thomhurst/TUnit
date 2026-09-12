using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Context for test registration phase
/// </summary>
public class TestRegisteredContext
{
    private List<ITestRegisteredEventReceiver>? _executorReceivers;
    private int _dispatchedExecutorReceiverCount;
    private IParallelLimit? _explicitParallelLimiter;

    public string TestName { get; }
    public string? CustomDisplayName { get; }
    public TestContext TestContext { get; }
    public DiscoveredTest DiscoveredTest { get; set; } = null!;

    public TestRegisteredContext(TestContext testContext)
    {
        TestContext = testContext;
        TestName = testContext.Metadata.TestDetails.TestName;
        CustomDisplayName = testContext.CustomDisplayName;
    }

    /// <summary>
    /// Gets the object bag from the underlying TestContext
    /// </summary>
    public ConcurrentDictionary<string, object?> StateBag => TestContext.StateBag.Items;

    /// <inheritdoc cref="StateBag"/>
    [Obsolete("Use StateBag property instead.")]
    public ConcurrentDictionary<string, object?> ObjectBag => StateBag;

    /// <summary>
    /// Gets the test details from the underlying TestContext
    /// </summary>
    public TestDetails TestDetails => TestContext.Metadata.TestDetails;

    public void SetTestExecutor(ITestExecutor executor)
    {
        DiscoveredTest.TestExecutor = executor;
        QueueExecutorEventReceiver(executor);
    }

    /// <summary>
    /// Sets a custom hook executor that will be used for all test-level hooks (Before/After Test).
    /// This allows you to wrap hook execution in custom logic (e.g., running on a specific thread).
    /// </summary>
    public void SetHookExecutor(IHookExecutor executor)
    {
        TestContext.CustomHookExecutor = executor;
        QueueExecutorEventReceiver(executor);
    }

    /// <summary>
    /// Sets the programmatic parallel limiter for the test. An explicit
    /// <see cref="ParallelLimiterAttribute{TParallelLimit}"/> takes precedence regardless of callback order.
    /// </summary>
    public void SetParallelLimiter(IParallelLimit parallelLimit)
    {
        TestContext.ParallelLimiter = _explicitParallelLimiter ?? parallelLimit;
    }

    internal void SetExplicitParallelLimiter(IParallelLimit parallelLimit)
    {
        _explicitParallelLimiter = parallelLimit;
        TestContext.ParallelLimiter = parallelLimit;
    }

    /// <summary>
    /// Marks the test as skipped with the specified reason.
    /// This can only be called during the test registration phase.
    /// </summary>
    /// <param name="reason">The reason why the test is being skipped</param>
    public void SetSkipped(string reason)
    {
        TestContext.SkipReason = reason;
        TestContext.Metadata.TestDetails.ClassInstance = SkippedTestInstance.Instance;
    }

    /// <summary>
    /// Queues an executor that is itself an <see cref="ITestRegisteredEventReceiver"/> for dispatch.
    /// </summary>
    /// <param name="executor">The executor a registration receiver has just installed.</param>
    /// <remarks>
    /// An executor installed during registration may not have been collected by the engine's
    /// eligible-object pass. An executor that is both an <see cref="ITestExecutor"/> and an
    /// <see cref="IHookExecutor"/> arrives through two calls and is queued once.
    /// </remarks>
    private void QueueExecutorEventReceiver(object executor)
    {
        if (executor is not ITestRegisteredEventReceiver receiver)
        {
            return;
        }

        _executorReceivers ??= [];

        // Identity, not equality: two distinct executors that compare equal each own their callback.
        foreach (var queued in _executorReceivers)
        {
            if (ReferenceEquals(queued, receiver))
            {
                return;
            }
        }

        _executorReceivers.Add(receiver);
    }

    /// <summary>
    /// Dequeues the next executor awaiting its registration callback.
    /// </summary>
    /// <param name="receiver">When this method returns, contains the executor to dispatch to.</param>
    /// <returns><see langword="true"/> if an executor was awaiting dispatch; otherwise, <see langword="false"/>.</returns>
    /// <remarks>Each executor is returned once, however many times it was installed.</remarks>
    internal bool TryDequeueExecutorEventReceiver([NotNullWhen(true)] out ITestRegisteredEventReceiver? receiver)
    {
        if (_executorReceivers is null || _dispatchedExecutorReceiverCount == _executorReceivers.Count)
        {
            receiver = null;
            return false;
        }

        receiver = _executorReceivers[_dispatchedExecutorReceiverCount++];
        return true;
    }
}
