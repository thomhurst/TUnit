namespace TUnit.Engine.Scheduling;

/// <summary>
/// Default implementation of progress monitoring
/// </summary>
public sealed class DefaultProgressMonitor : IProgressMonitor
{
    private readonly TimeSpan _stallTimeout;
    private readonly TimeSpan _checkInterval;
    private readonly Action<string>? _onStallDetected;

    public DefaultProgressMonitor(
        TimeSpan? stallTimeout = null,
        TimeSpan? checkInterval = null,
        Action<string>? onStallDetected = null)
    {
        _stallTimeout = stallTimeout ?? TimeSpan.FromMinutes(10);
        _checkInterval = checkInterval ?? TimeSpan.FromSeconds(30);
        _onStallDetected = onStallDetected;
    }

    public async Task MonitorProgressAsync(TestCompletionTracker tracker, CancellationToken cancellationToken)
    {
        var lastProgress = DateTime.UtcNow;
        var lastCompletedCount = 0;

        while (!cancellationToken.IsCancellationRequested && !tracker.AllTestsCompleted)
        {
            try
            {
                await Task.Delay(_checkInterval, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }

            var currentCompleted = tracker.CompletedCount;
            if (currentCompleted > lastCompletedCount)
            {
                lastProgress = DateTime.UtcNow;
                lastCompletedCount = currentCompleted;
            }
            else if (DateTime.UtcNow - lastProgress > _stallTimeout)
            {
                var message = $"Test execution stalled - no progress in {_stallTimeout.TotalMinutes} minutes. " +
                    $"Completed: {currentCompleted}/{tracker.TotalCount}";

                _onStallDetected?.Invoke(message);

                throw new TimeoutException(message);
            }
        }
    }
}
