namespace TUnit.Engine.Scheduling;

/// <summary>
/// Batches work notifications to reduce overhead
/// </summary>
internal sealed class BatchedWorkNotifier : IDisposable
{
    private readonly WorkNotificationSystem _notificationSystem;
    private readonly Timer _batchTimer;
    private int _pendingNotifications;
    
    public BatchedWorkNotifier(WorkNotificationSystem notificationSystem)
    {
        _notificationSystem = notificationSystem;
        _batchTimer = new Timer(FlushNotifications, null, Timeout.Infinite, Timeout.Infinite);
    }
    
    public void NotifyWorkAvailable()
    {
        if (Interlocked.Increment(ref _pendingNotifications) == 1)
        {
            // First notification, start batch timer (100 microseconds = 0.1 milliseconds)
            _batchTimer.Change(TimeSpan.FromTicks(1), Timeout.InfiniteTimeSpan);
        }
    }
    
    private async void FlushNotifications(object? state)
    {
        var count = Interlocked.Exchange(ref _pendingNotifications, 0);
        if (count > 0)
        {
            // Send single notification for batch
            await _notificationSystem.NotifyWorkAvailableAsync(
                new WorkNotification 
                { 
                    Source = WorkNotification.WorkSource.GlobalQueue,
                    Priority = count // Use count as priority hint
                });
        }
    }
    
    public void Dispose()
    {
        _batchTimer?.Dispose();
    }
}