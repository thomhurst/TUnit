using System.Threading.Channels;

namespace TUnit.Engine.Events;

/// <summary>
/// Batches events for efficient processing
/// </summary>
internal sealed class EventBatcher<TEvent> : IAsyncDisposable, IDisposable where TEvent : class
{
    private readonly Channel<TEvent> _eventChannel;
    private readonly Func<IReadOnlyList<TEvent>, ValueTask> _batchProcessor;
    private readonly Task _processingTask;
    private readonly CancellationTokenSource _shutdownCts = new();
    
    public EventBatcher(
        Func<IReadOnlyList<TEvent>, ValueTask> batchProcessor,
        int batchSize = 100,
        TimeSpan maxBatchDelay = default)
    {
        _batchProcessor = batchProcessor;
        _eventChannel = Channel.CreateUnbounded<TEvent>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
        
        _processingTask = ProcessBatchesAsync(
            batchSize, 
            maxBatchDelay == TimeSpan.Zero ? TimeSpan.FromMilliseconds(10) : maxBatchDelay,
            _shutdownCts.Token);
    }
    
    public async ValueTask EnqueueEventAsync(TEvent evt)
    {
        if (!_shutdownCts.Token.IsCancellationRequested)
        {
            await _eventChannel.Writer.WriteAsync(evt);
        }
    }
    
    public bool TryEnqueueEvent(TEvent evt)
    {
        if (!_shutdownCts.Token.IsCancellationRequested)
        {
            return _eventChannel.Writer.TryWrite(evt);
        }
        return false;
    }
    
    private async Task ProcessBatchesAsync(
        int batchSize, 
        TimeSpan maxDelay,
        CancellationToken cancellationToken)
    {
        var batch = new List<TEvent>(batchSize);
        
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                batch.Clear();
                
                // Try to fill batch up to batchSize or until timeout
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(maxDelay);
                
                try
                {
                    // Fill batch
                    while (batch.Count < batchSize && !timeoutCts.Token.IsCancellationRequested)
                    {
                        if (await _eventChannel.Reader.WaitToReadAsync(timeoutCts.Token))
                        {
                            if (_eventChannel.Reader.TryRead(out var evt))
                            {
                                batch.Add(evt);
                            }
                        }
                    }
                }
                catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                {
                    // Timeout expired, process what we have
                }
                
                // Process batch if we have events
                if (batch.Count > 0)
                {
                    try
                    {
                        await _batchProcessor(batch);
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue processing
                        Console.Error.WriteLine($"EventBatcher: Error processing batch: {ex.Message}");
                    }
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected during shutdown
        }
        finally
        {
            // Process any remaining events
            if (batch.Count > 0)
            {
                try
                {
                    await _batchProcessor(batch);
                }
                catch
                {
                    // Best effort during shutdown
                }
            }
            
            // Drain any remaining events
            while (_eventChannel.Reader.TryRead(out var evt))
            {
                batch.Add(evt);
                if (batch.Count >= batchSize)
                {
                    try
                    {
                        await _batchProcessor(batch);
                        batch.Clear();
                    }
                    catch
                    {
                        // Best effort during shutdown
                    }
                }
            }
            
            if (batch.Count > 0)
            {
                try
                {
                    await _batchProcessor(batch);
                }
                catch
                {
                    // Best effort during shutdown
                }
            }
        }
    }
    
    public async ValueTask FlushAsync()
    {
        _eventChannel.Writer.TryComplete();
        await _processingTask.ConfigureAwait(false);
    }
    
    public async ValueTask DisposeAsync()
    {
        _eventChannel.Writer.TryComplete();
        _shutdownCts.Cancel();
        
        try
        {
            // Properly await the task with timeout
#if NET6_0_OR_GREATER
            await _processingTask.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
#else
            // For .NET Framework, use Task.WhenAny to implement timeout
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var completedTask = await Task.WhenAny(_processingTask, Task.Delay(Timeout.Infinite, cts.Token)).ConfigureAwait(false);
            if (completedTask == _processingTask)
            {
                await _processingTask.ConfigureAwait(false);
            }
            // If timeout occurred, we just continue without waiting
#endif
        }
        catch
        {
            // Best effort shutdown
        }
        
        _shutdownCts.Dispose();
    }
    
    public void Dispose()
    {
        // Synchronous dispose - best effort without blocking
        _eventChannel.Writer.TryComplete();
        _shutdownCts.Cancel();
        _shutdownCts.Dispose();
        
        // Note: For proper cleanup, use DisposeAsync() instead
    }
}