using System.Threading.Channels;

namespace TUnit.Engine.Scheduling;

/// <summary>
/// Efficient work notification system for test workers
/// </summary>
internal sealed class WorkNotificationSystem : IDisposable
{
    private readonly Channel<WorkNotification> _workChannel;
    private readonly SemaphoreSlim _workAvailable;
    private readonly CancellationTokenSource _shutdownSource = new();
    
    public WorkNotificationSystem(int maxPendingNotifications = 1000)
    {
        // Bounded channel prevents runaway memory usage
        _workChannel = Channel.CreateBounded<WorkNotification>(
            new BoundedChannelOptions(maxPendingNotifications)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = false
            });
            
        _workAvailable = new SemaphoreSlim(0);
    }
    
    /// <summary>
    /// Notify workers that new work is available
    /// </summary>
    public async ValueTask NotifyWorkAvailableAsync(
        WorkNotification notification,
        CancellationToken cancellationToken = default)
    {
        await _workChannel.Writer.WriteAsync(notification, cancellationToken);
        _workAvailable.Release();
    }
    
    /// <summary>
    /// Wait for work to become available
    /// </summary>
    public async ValueTask<WorkNotification?> WaitForWorkAsync(
        CancellationToken cancellationToken = default)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, _shutdownSource.Token);
            
        try
        {
            await _workAvailable.WaitAsync(linkedCts.Token);
            
            if (_workChannel.Reader.TryRead(out var notification))
            {
                return notification;
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown
        }
        
        return null;
    }
    
    /// <summary>
    /// Complete work notifications
    /// </summary>
    public void CompleteNotifications()
    {
        _workChannel.Writer.TryComplete();
    }
    
    public void Dispose()
    {
        _shutdownSource.Cancel();
        _shutdownSource.Dispose();
        _workAvailable.Dispose();
    }
}

/// <summary>
/// Notification that work is available
/// </summary>
internal readonly struct WorkNotification
{
    public WorkSource Source { get; init; }
    public int Priority { get; init; }
    
    public enum WorkSource
    {
        GlobalQueue,
        LocalQueue,
        NewlyReady,
        Stolen
    }
}