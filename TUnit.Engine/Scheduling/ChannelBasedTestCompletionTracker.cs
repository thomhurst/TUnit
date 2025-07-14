using System.Collections.Concurrent;
using System.Threading.Channels;
using TUnit.Core;
using TUnit.Core.Enums;
using TUnit.Core.Helpers;

namespace TUnit.Engine.Scheduling;

/// <summary>
/// Channel-based test completion tracker with built-in async notification
/// </summary>
internal sealed class ChannelBasedTestCompletionTracker : IDisposable
{
    private readonly ConcurrentDictionary<string, TestExecutionState> _graph;
    private readonly Channel<TestExecutionState> _readyChannel;
    private readonly Counter _completedCounter = new();
    private readonly int _totalCount;
    
    public ChannelBasedTestCompletionTracker(
        Dictionary<string, TestExecutionState> graph,
        int channelCapacity = 1000)
    {
        _graph = new ConcurrentDictionary<string, TestExecutionState>(graph);
        _totalCount = graph.Count;
        
        // Bounded channel for backpressure
        _readyChannel = Channel.CreateBounded<TestExecutionState>(new BoundedChannelOptions(channelCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleWriter = false,
            SingleReader = false,
            AllowSynchronousContinuations = false
        });
    }
    
    public int CompletedCount => _completedCounter.CurrentCount;
    public int TotalCount => _totalCount;
    public bool AllTestsCompleted => _completedCounter.CurrentCount >= _totalCount;
    
    /// <summary>
    /// Get the channel reader for consuming ready tests
    /// </summary>
    public ChannelReader<TestExecutionState> ReadyTests => _readyChannel.Reader;
    
    /// <summary>
    /// Try to enqueue a test that's ready to run
    /// </summary>
    public bool TryEnqueueReady(TestExecutionState test)
    {
        return _readyChannel.Writer.TryWrite(test);
    }
    
    /// <summary>
    /// Enqueue a test that's ready to run (async)
    /// </summary>
    public async ValueTask EnqueueReadyAsync(TestExecutionState test, CancellationToken cancellationToken = default)
    {
        await _readyChannel.Writer.WriteAsync(test, cancellationToken);
    }
    
    /// <summary>
    /// Process test completion and enqueue newly ready dependents
    /// </summary>
    public async ValueTask OnTestCompletedAsync(TestExecutionState completedTest, CancellationToken cancellationToken = default)
    {
        _completedCounter.Increment();
        
        // Process dependents
        foreach (var dependentId in completedTest.Dependents)
        {
            if (_graph.TryGetValue(dependentId, out var dependentState))
            {
                var remaining = dependentState.DecrementRemainingDependencies();
                
                if (remaining == 0 && dependentState.State == TestState.NotStarted)
                {
                    // Test is ready to run - write directly to channel
                    // This provides automatic notification to waiting workers
                    await _readyChannel.Writer.WriteAsync(dependentState, cancellationToken);
                }
            }
        }
        
        // Complete the channel when all tests are done
        if (AllTestsCompleted)
        {
            _readyChannel.Writer.TryComplete();
        }
    }
    
    /// <summary>
    /// Complete the ready channel (no more tests will be added)
    /// </summary>
    public void Complete()
    {
        _readyChannel.Writer.TryComplete();
    }
    
    /// <summary>
    /// Wait for the next ready test
    /// </summary>
    public async ValueTask<TestExecutionState?> WaitForReadyTestAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (await _readyChannel.Reader.WaitToReadAsync(cancellationToken))
            {
                if (_readyChannel.Reader.TryRead(out var test))
                {
                    return test;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during cancellation
        }
        
        return null;
    }
    
    public void Dispose()
    {
        _readyChannel.Writer.TryComplete();
    }
}